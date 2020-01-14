using FluentAssertions;
using Newtonsoft.Json;
using System;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class ModelSerialisationTests
    {
        [Fact]
        public void A()
        {
            var a = Resources.LoadAsString(
                "SFA.DAS.Zendesk.Monitor.UnitTests.TestData.zendesk.ticket.1017.json");

            var j = JsonConvert.DeserializeObject<Zendesk.Model.TicketResponse>(
                a, Zendesk.ApiFactoryExtensions.serialiser);

            j.Ticket.Should().BeEquivalentTo(new
            {
                Id = 1017,
                CreatedAt = DateTimeOffset.Parse("2019-12-10T15:35:49Z"),
                UpdatedAt = DateTimeOffset.Parse("2019-12-10T17:03:24Z"),
                Type = "question",
                Subject = "SQRL TEST - CRM SOLVE Test",
                Description = "test",
                Priority = "low",
                Status = "solved",
                RequesterId = 363003813860,
                OrganizationId = 362277482460,
                Tags = new[]
                {
                    "hmrc____government_gateway_paye_link",
                    "query",
                },
            });

            j.Ticket.CustomFields.Should().ContainEquivalentOf(
                new { Id = 360004171439, Value = "INC01167381" }
                );

            j.Users.Should().ContainEquivalentOf(new
            {
                Id = 362626196619,
                Name = "Shakil Mahmood",
                Email = "shakil.mahmood@digital.education.gov.uk",
                CreatedAt = DateTimeOffset.Parse("2019-09-04T19:26:12Z"),
                UpdatedAt = DateTimeOffset.Parse("2019-12-12T15:11:26Z"),
                Phone = "01215555555",
                UserFields = new
                {
                    AddressLine1 = "12b Baker Street",
                    AddressLine2 = "Somewhere",
                    AddressLine3 = "",
                    City = "Hull",
                    Postcode = "hu1 1uh",
                    ContactType = "employer",
                }
            });

            j.Organizations.Should().BeEquivalentTo(new
            {
                Name = "ShakTestOrg",
                CreatedAt = DateTimeOffset.Parse("2019-10-07T13:51:06Z"),
                UpdatedAt = DateTimeOffset.Parse("2019-12-10T16:13:58Z"),
                OrganizationFields = new
                {
                    AddressLine1 = "123",
                    AddressLine2 = "testing street",
                    AddressLine3 = "north london",
                    City = "harringey",
                    County = "Armagh",
                    Postcode = "n4 1qx",
                    MainPhone = "01234567890",
                    OrganisationStatus = "active",
                    OrganisationType = "employer",
                },
            });
        }
        
        [Fact]
        public void Main_phone_with_alpha_characters()
        {
            var a = Resources.LoadAsString(
                "SFA.DAS.Zendesk.Monitor.UnitTests.TestData.zendesk.ticket.1086.main_phone_with_alpha.json");

            var j = JsonConvert.DeserializeObject<Zendesk.Model.TicketResponse>(
                a, Zendesk.ApiFactoryExtensions.serialiser);

            j.Users.Should().BeEquivalentTo(new
            {
                Id = 6619,
                Phone = "01234 666 777 and some letters",
            });


            j.Organizations.Should().BeEquivalentTo(new
            {
                Name = "Org with alphabetic Main Phone field",
                OrganizationFields = new
                {
                    MainPhone = "01234 567 890 and some letters",
                },
            });
        }
    }
}
