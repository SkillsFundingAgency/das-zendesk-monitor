using AutoFixture;
using AutoMapper;
using SFA.DAS.Zendesk.Monitor.Middleware;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
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

            {
                var ticket = DefaultZendeskTicket();
                ticket.Users[0].UserFields.AddressLine1 = null;
                ticket.Users[0].UserFields.AddressLine2 = null;
                ticket.Users[0].UserFields.AddressLine3 = null;
                yield return ticket;
            }

            {
                var ticket = DefaultZendeskTicket();
                ticket.Ticket.CustomFields[0].Value = null;
                yield return ticket;
            }

            {
                var minimalTicket = new TicketResponse
                {
                    Ticket = new Ticket
                    {
                        RequesterId = 1,
                        OrganizationId = 1,
                        Subject = "Ticket ticket ticket",
                        Description = "...",
                        Via = new Via
                        {
                            Channel = "email",
                        }
                    },
                    Users = new []
                    {
                        new User
                        {
                            Id = 1,
                            Name = "me",
                            UserFields = new UserFields { },
                        }
                    },
                    Organizations = new []
                    {
                        new Organization
                        {
                            Id = 1,
                            Name = "BobCo",
                            OrganizationFields = new OrganizationFields 
                            {
                                // TODO - this may be null / empty from Zendesk,
                                //        but the schema mandates it's one of 
                                //        its enum values
                                OrganisationStatus = "active",
                            },
                        },
                    },
                };
                yield return minimalTicket;
            }
        }

        private static TicketResponse DefaultZendeskTicket()
        {
            var fixture = new Fixture();
            var ticket = fixture.Create<Zendesk.Model.TicketResponse>();

            ticket.Ticket.Via.Channel = "email";

            ticket.Ticket.OrganizationId = ticket.Organizations[0].Id;
            ticket.Organizations[0].OrganizationFields.OrganisationType = "";
            ticket.Organizations[0].OrganizationFields.OrganisationStatus = "active";

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
