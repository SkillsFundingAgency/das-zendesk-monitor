using Newtonsoft.Json;
using Newtonsoft.Json.Serialization.ContractResolverExtentions;
using RestEase;
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
using ZenWatch.Zendesk;

namespace ZenWatch.Acceptance.Fakes
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

        public static JsonSerializerSettings JsonSerializerSettings { get; } =
            new JsonSerializerSettings
            {
                ContractResolver = new SnakeCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
            };

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

        public FakeZendesk()
        {
            /*
            var url = server.Urls.First();
            /*/
            var url = "https://esfa.zendesk.com";
            /**/

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri($"{url}/api/v2");
            httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };

            var byteArray = Encoding.ASCII.GetBytes("ben.arnold@digital.education.gov.uk/token:xxxxxxxxxx");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            zendeskApi = RestClient.For<IApi>(httpClient);//,
                                                          //new
                                                          //{
                                                          //    ContentSerializer = new JsonContentSerializer(JsonSerializerSettings),
                                                          //});
        }

        internal async Task<Ticket> GetTicket(long id) => (await zendeskApi.GetTicket(id)).Ticket;

        internal async Task<Ticket> CreateTicket()
        {
            var ticket = new Ticket
            {
                Subject = $"Integration testing Watcher",
                Comment = new Comment { Body = "Created for testing" },
            };

            var response = await zendeskApi.PostTicket(new Empty { Ticket = ticket });
            return response.Ticket;
        }

        internal Task UpdateTicket(Ticket ticket)
        {
            return zendeskApi.PutTicket(ticket.Id, new Empty { Ticket = ticket });
        }

        public async Task<Ticket[]> /*ISharingTickets.*/GetTicketsForSharing()
        {
            var response = await zendeskApi.SearchTickets("tags:pending_middleware");
            return response.Results;
        }

        Task ISharingTickets.MarkShared(Ticket ticket)
        {
            ticket.Tags.Remove("pending_middleware");
            return zendeskApi.PutTicket(ticket.Id, new Empty { Ticket = ticket });
        }

        Task ISharingTickets.MarkSharing(Ticket t)
        {
            return Task.CompletedTask;
        }
    }
}