using AutoFixture.Xunit2;
using FluentAssertions;
using Newtonsoft.Json.Schema;
using RestEase;
using SFA.DAS.Zendesk.Monitor.Middleware;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests.Tests
{
    public class MiddlewareSchemaTests
    {
        [Theory, AutoData]
        public async Task FullyGeneratedEventWrapperPassesValidation(EventWrapper wrapper)
        {
            wrapper.Ticket.Organization.OrganizationFields.OrganisationStatus = "active";
            wrapper.Ticket.Organization.OrganizationFields.OrganisationType = "";
            wrapper.Ticket.Requester.UserFields.ContactType = "";
            wrapper.Ticket.Via = "Web Form";

            // Given
            var handler = new HttpMessageHandlerSpy();
            var api = new RestClient(new HttpClient(handler)).CreateApi();

            // When
            await api.EscalateTicket(wrapper);

            // Then
            handler.Requests.Should()
                .Contain(x => x.RequestUri.ToString().Contains("/ticket/")).Which
                .Should().HavePayloadValidatedBy(MiddlewareSchema);
        }

        [Theory, ClassData(typeof(MiddlewareSchemaTestData))]
        public async Task ZendeskTicketMappedToEventWrapperPassesValidation(EventWrapper wrapper)
        {
            // Given
            var spy = new HttpMessageHandlerSpy();
            var api = new RestClient(new HttpClient(spy)).CreateApi();

            // When
            await api.EscalateTicket(wrapper);

            // Then
            spy.Requests.Should()
                .Contain(x => x.RequestUri.ToString().Contains("/ticket/")).Which
                .Should().HavePayloadValidatedBy(MiddlewareSchema);

        }

        private JSchema MiddlewareSchema =>
        JSchema.Parse(
            Resources.LoadAsString(
                "SFA.DAS.Zendesk.Monitor.UnitTests.TestData.MiddlewareSchema-20191204.json"));
    }
}
