using AutoFixture.Xunit2;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class ModelSerialisationTests
    {
        private const string iso8601 = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";

        [Fact]
        public void SmokeTestDeserialisationOfCapturedTicket()
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

        [Theory]
        [InlineData(@"""123456""", "123456")]
        [InlineData(@"[""123"", ""456""]", "123,456")]
        [InlineData(@"{""minutes"":3600,""in_business_hours"":true}",
            "{\"minutes\":3600,\"in_business_hours\":true}")]
        [InlineData(@"123456", "123456")]
        [InlineData(@"null", null)]
        public void TestCustomDeserialisationOfEventValue2(
            string value,
            string expected)
        {
            var json = @"{""audits"": [{""events"": [" +
                @"{""value"": " + value + "}," +
                @"{""previous_value"": " + value + "}" +
                @"]}]}";

            var j = JsonConvert.DeserializeObject<Zendesk.Model.AuditResponse>(
                json, Zendesk.ApiFactoryExtensions.serialiser);

            j.Audits.SelectMany(x => x.Events).Should()
                .ContainEquivalentOf(new { Value = expected });
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

        [Theory, AutoData]
        public void Serialise_SafeModifyTags(SafeModifyTags ticket)
        {
            var json = JsonConvert.SerializeObject(
                ticket, Zendesk.ApiFactoryExtensions.serialiser);

            var expected = $"{{\"ticket\":{{" +
                $"\"safe_update\":true," +
                $"\"updated_stamp\":\"{ticket.UpdatedStamp.ToString(iso8601)}\"," +
                $"\"tags\":[{ticket.Tags.ToQuotedList()}]" +
                $"}}}}";

            json.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void Serialise_CustomFields_with_complex_object()
        {
            var a = Resources.LoadAsString(
                "SFA.DAS.Zendesk.Monitor.UnitTests.TestData.zendesk.ticket.39702.json");

            var j = JsonConvert.DeserializeObject<Zendesk.Model.TicketResponse>(
                a, Zendesk.ApiFactoryExtensions.serialiser);

            j.Ticket.CustomFields.Should().ContainEquivalentOf(
                new { Id = 360002764900, Value = new JArray("7") }
                );
        }
    }

    public static class StringExtensions
    {
        public static string ToQuotedList(this IEnumerable<string> xs)
            => string.Join(",", xs.Select(x => $"\"{x}\""));
    }
}