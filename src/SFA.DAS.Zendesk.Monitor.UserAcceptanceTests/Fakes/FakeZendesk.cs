using LanguageExt;
using Microsoft.Extensions.Configuration;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
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
        }

        internal async Task<Ticket> GetTicket(long id) => (await zendeskApi.GetTicket(id)).Ticket;

        internal async Task<Ticket> CreateTicket()
        {
            var ticket = new Ticket
            {
                Subject = $"Integration testing Watcher",
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
            ticket.CustomFields.FirstOrDefault(x => x.Id == 360004171339).Value = "customer_disconnect";
            ticket.CustomFields.FirstOrDefault(x => x.Id == 360004146580).Value = "as_end_point_assessors";
            ticket.CustomFields.FirstOrDefault(x => x.Id == 360009649760).Value = "esfa_apprenticeship_dev_ops_";

            await zendeskApi.UpdateTicket(ticket.Id, new TicketRequest { Ticket = ticket });
        }

        internal Task AddTag(Ticket ticket, string v)
            => zendeskApi.ModifyTags(ticket, additions: new[] { v });

        Task<long[]> ISharingTickets.GetTicketsForSharing() => sharing.GetTicketsForSharing();

        Task<Option<SharedTicket>> ISharingTickets.GetTicketForSharing(long id) => sharing.GetTicketForSharing(id);

        Task ISharingTickets.MarkShared(SharedTicket share) => sharing.MarkShared(share);

        Task ISharingTickets.MarkSharing(SharedTicket share) => sharing.MarkSharing(share);
    }
}