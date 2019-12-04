using AutoFixture;
using AutoMapper;
using SFA.DAS.Zendesk.Monitor.Middleware;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SFA.DAS.Zendesk.Monitor.UnitTests.Tests
{
    public class MiddlewareSchemaTestData : IEnumerable<object[]>
    {
        static readonly IMapper mapperConfiguration =
            new MapperConfiguration(cfg => cfg.AddProfile<TicketProfile>())
            .CreateMapper(); 
        
        public IEnumerator<object[]> GetEnumerator() 
            => ZendeskTicketData()
                .Select(ConvertZendeskToMiddleware)
                .GetEnumerator();

        private static IEnumerable<TicketResponse> ZendeskTicketData()
        {
            {
                yield return DefaultZendeskTicket();
            }

            {
                var ticket = DefaultZendeskTicket();
                ticket.Organizations[0].OrganizationFields.AddressLine1 = null;
                ticket.Organizations[0].OrganizationFields.AddressLine2 = null;
                ticket.Organizations[0].OrganizationFields.AddressLine3 = null;
                yield return ticket;
            }
        }

        private static TicketResponse DefaultZendeskTicket()
        {
            var fixture = new Fixture();
            var ticket = fixture.Create<Zendesk.Model.TicketResponse>();

            ticket.Ticket.Via.Channel = "email";

            ticket.Ticket.OrganizationId = ticket.Organizations[0].Id;
            ticket.Organizations[0].OrganizationFields.OrganizationType = "";
            ticket.Organizations[0].OrganizationFields.OrganizationStatus = "active";

            ticket.Ticket.RequesterId = ticket.Users[0].Id;
            ticket.Users[0].UserFields.ContactType = "";

            return ticket;
        }

        private static object[] ConvertZendeskToMiddleware(TicketResponse t)
            => new object[]
            {
                mapperConfiguration.Map<EventWrapper>(t)
            };

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
