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
        };

        [Theory, MemberData(nameof(ViaTestData))]
        public void TestViaMapping(Via via, string viaText)
        {
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile<TicketProfile>()).CreateMapper();

            var ticket = new Ticket { Via = via };

            var mapped = mapper.Map<Middleware.Model.Ticket>(ticket);

            mapped.Via.Should().Be(viaText);
        }
    }
}
