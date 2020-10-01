using AutoMapper;
using FluentAssertions;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Collections.Generic;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class MappingProfileTests
    {
        [Fact]
        public void TestProfileIsValid()
        {
            var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<TicketProfile>());
            mapperConfig.AssertConfigurationIsValid();
        }

        public static IEnumerable<object[]> ViaTestData => new List<object[]>
        {
            new object[]
            {
                new Via
                {
                    Channel = "voice",
                    Source = new Source { Rel = "inbound", },
                },
                "Phone call (inbound)"
            },
            new object[]
            {
                new Via
                {
                    Channel = "voice",
                    Source = new Source { Rel = "voicemail", },
                },
                "Voicemail"
            },
            new object[]
            {
                new Via
                {
                    Channel = "voice",
                    Source = new Source { Rel = "outbound", },
                },
                "Phone call (outbound)"
            },
            new object[]
            {
                new Via {Channel = "email"},
                "Mail"
            },
            new object[]
            {
                new Via { Channel = "chat" },
                "Chat"
            },
            new object[]
            {
                new Via { Channel = "web" },
                "Web Form"
            },
            new object[]
            {
                new Via { Channel = "twitter" },
                "Mail"
            },
            new object[]
            {
                new Via { Channel = "api" },
                "Mail"
            },
        };

        [Theory, MemberData(nameof(ViaTestData))]
        public void TestViaMapping(Via via, string viaText)
        {
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile<TicketProfile>()).CreateMapper();

            var ticket = new Ticket { Via = via };

            var mapped = mapper.Map<Middleware.Model.Ticket>(ticket);

            mapped.Via.Should().Be(viaText);
        }

        [Theory]
        [InlineData("middleware_destination_itsm", "itsm")]
        [InlineData("middleware_destination_pcrm", "pcrm")]
        [InlineData("some_other_tag", "itsm")] // default to "itsm"
        [InlineData("middleware_destination_", "")]  // ability to "unset" destination
        public void TestDestinationMapping(string tag, string destination)
        {
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile<TicketProfile>()).CreateMapper();

            var ticket = new Ticket
            {
                Tags = new List<string> { tag },
            };

            var mapped = mapper.Map<Middleware.Model.Ticket>(ticket);

            mapped.Destination.Should().Be(destination);
        }
    }
}