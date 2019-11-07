using AutoMapper;
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
    }
}
