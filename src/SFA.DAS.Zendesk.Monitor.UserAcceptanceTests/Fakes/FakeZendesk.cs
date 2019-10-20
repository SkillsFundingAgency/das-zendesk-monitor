using LanguageExt;
using Microsoft.Extensions.Configuration;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.IO;
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

        private readonly FluentMockServer server = FluentMockServer.Start(new FluentMockServerSettings
        {
            //*
            ReadStaticMappings = true,
            FileSystemHandler = new LocalFileSystemHandler(ProjectPath),
            /*/
            ProxyAndRecordSettings = new ProxyAndRecordSettings
            {
                Url = "https://esfa.zendesk.com/api/v2",
                SaveMapping = true,
                SaveMappingToFile = true,
                BlackListedHeaders = new[] { "dnt", "Content-Length", "Authorization", "Host" }
            }
            /**/
        });

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

            /*
            var url = server.Urls.First();
            /*/
            var url = $"https://{instance}.zendesk.com";
            /**/

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri($"{url}/api/v2");
            httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };

            var byteArray = Encoding.ASCII.GetBytes($"{user}/token:{token}");
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

            var response = await zendeskApi.PostTicket(ticket);
            return response.Ticket;
        }

        internal Task UpdateTicket(Ticket ticket)
        {
            return zendeskApi.PutTicket(ticket);
        }

        public Task<long[]> /*ISharingTickets.*/GetTicketsForSharing() => sharing.GetTicketsForSharing();

        Task<Option<(TicketResponse, SharingReason)>> ISharingTickets.GetTicketForSharing(long id) => sharing.GetTicketForSharing(id);

        Task ISharingTickets.MarkShared(Ticket ticket, SharingReason reason) => sharing.MarkShared(ticket, reason);

        Task ISharingTickets.MarkSharing(Ticket t, SharingReason reason) => sharing.MarkSharing(t, reason);
    }
}