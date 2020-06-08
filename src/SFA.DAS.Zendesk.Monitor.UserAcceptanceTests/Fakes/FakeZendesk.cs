using LanguageExt;
using Microsoft.Extensions.Configuration;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WireMock.Handlers;
using WireMock.Server;
using WireMock.Settings;

namespace SFA.DAS.Zendesk.Monitor.Acceptance.Fakes
{
    public class FakeZendesk : ISharingTickets
    {
        private static string ProjectPath
        {
            get
            {
                var pathRegex = new Regex(@"\\bin(\\x86|\\x64)?\\(Debug|Release)(\\netcoreapp.*)?$", RegexOptions.Compiled);
                var directory = pathRegex.Replace(Directory.GetCurrentDirectory(), "");
                return Path.Combine(directory, "WireMockMappings");
            }
        }

        private static ProxyAndRecordSettings LiveZendeskProxySettings(string instance) => new ProxyAndRecordSettings
        {
            Url = $"https://{instance}.zendesk.com/api/v2",
            SaveMapping = true,
            SaveMappingToFile = true,
            BlackListedHeaders = new[] { "dnt", "Content-Length", "Authorization", "Host" }
        };

        private readonly FluentMockServer server;
        private readonly IApi zendeskApi;
        private readonly ISharingTickets sharing;
        private readonly Lazy<Task<Dictionary<string, long>>> ticketFieldIds;

        public FakeZendesk()
        {
            var conf = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.local.json", optional: true)
                .Build();

            var instance = conf.GetValue<string>("Zendesk:Instance");
            var user = conf.GetValue<string>("Zendesk:ApiUser");
            var token = conf.GetValue<string>("Zendesk:ApiKey");
            var useLiveZendesk = conf.GetValue<bool?>("Zendesk:UseLiveInstance") ?? false;

            server = FluentMockServer.Start(new FluentMockServerSettings
            {
                ReadStaticMappings = !useLiveZendesk,
                FileSystemHandler = new LocalFileSystemHandler(ProjectPath),
                ProxyAndRecordSettings = useLiveZendesk ? LiveZendeskProxySettings(instance) : null,
            });

            var url = server.Urls.First();

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri($"{url}/api/v2");
            httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };

            var byteArray = Encoding.ASCII.GetBytes($"{user}:{token}");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            zendeskApi = ApiFactory.CreateApi(httpClient);
            sharing = new SharingTickets(zendeskApi);
            ticketFieldIds = new Lazy<Task<Dictionary<string, long>>>(LoadTicketFields);
        }

        private async Task<Dictionary<string, long>> LoadTicketFields()
        {
            var fields = await zendeskApi.GetTicketFieldIds();
            return fields.TicketFields
                .Where(x => x.Active)
                .Distinct(new UniqueTicketFieldTitle())
                .ToDictionary(x => x.Title, x => x.Id);
        }

        internal async Task<Ticket> GetTicket(long id) => (await zendeskApi.GetTicket(id)).Ticket;

        internal async Task<Ticket> CreateTicket()
        {
            var ticket = new Ticket
            {
                Subject = $"Integration testing Watcher {Guid.NewGuid()}",
                Comment = new Comment { Body = "Created for testing" },
            };

            var response = await zendeskApi.PostTicket(new TicketRequest { Ticket = ticket });
            return response.Ticket;
        }

        internal async Task Escalate(Ticket ticket)
        {
            // as per https://esfa1567428279.zendesk.com/agent/admin/macros/360016445439
            ticket.Tags.Add("escalated_tag");
            ticket.Comment = new Comment
            {
                Public = false,
                Body = "this ticket is being escalated by an automated test, please ignore.",
            };

            ticket.Status = "hold";
            await PopulateTicketCustomField(ticket, "Contact Reason", "customer_disconnect");
            await PopulateTicketCustomField(ticket, "Service Offering", "as_end_point_assessors");
            await PopulateTicketCustomField(ticket, "Resolver Group", "esfa_apprenticeship_dev_ops_");

            await zendeskApi.UpdateTicket(ticket.Id, new TicketRequest { Ticket = ticket });
        }

        private async Task PopulateTicketCustomField(Ticket ticket, string field, string value)
        {
            var fieldId = await CustomTicketFieldId(field);
            var ticketField = ticket.CustomField(fieldId);
            
            if(ticketField == null)
                throw new Exception($"Field {fieldId} (`{field}`) not found in ticket {ticket.Id}");

            ticketField.Value = value;
        }

        internal async Task<long> CustomTicketFieldId(string field)
        {
            var fieldIds = await ticketFieldIds.Value;

            return fieldIds.TryGetValue(field, out var fieldId)
                ? fieldId
                : throw new Exception($"Field `{field}` not found in \n{String.Join(",", fieldIds.Keys)}");
        }

        internal async Task Solve(Ticket ticket, string comment = "this ticket is has been solved by an automated test.")
        {
            ticket.Status = "solved";
            ticket.Comment = new Comment
            {
                Public = false,
                Body = comment,
            };
         
            await zendeskApi.UpdateTicket(ticket.Id, new TicketRequest { Ticket = ticket });
        }

        internal Task AddTag(Ticket ticket, string v)
            => zendeskApi.ModifyTags(ticket, additions: new[] { v });

        Task<long[]> ISharingTickets.GetTicketsForSharing() => sharing.GetTicketsForSharing();

        Task<Option<SharedTicket>> ISharingTickets.GetTicketForSharing(long id) => sharing.GetTicketForSharing(id);

        Task ISharingTickets.MarkShared(SharedTicket share) => sharing.MarkShared(share);

        Task ISharingTickets.MarkSharing(SharedTicket share) => sharing.MarkSharing(share);
    }

    internal class UniqueTicketFieldTitle : IEqualityComparer<TicketField>
    {
        public bool Equals([AllowNull] TicketField x, [AllowNull] TicketField y) =>
            x?.Title?.Equals(y?.Title) ?? false;

        public int GetHashCode([DisallowNull] TicketField obj) =>
            obj.Title.GetHashCode();
    }
}