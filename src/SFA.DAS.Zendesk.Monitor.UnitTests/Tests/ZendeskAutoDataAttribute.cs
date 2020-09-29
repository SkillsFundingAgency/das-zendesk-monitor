using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using SFA.DAS.Zendesk.Monitor.Zendesk;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class ZendeskAutoDataAttribute : AutoDataAttribute
    {
        public ZendeskAutoDataAttribute() : base(() => Customise())
        {
        }

        private static IFixture Customise()
        {
            var fixture = new Fixture();
            fixture.Register<IApi>(() => fixture.Create<FakeZendeskApi>());
            fixture.Register<ISharingTickets>(() => fixture.Create<SharingTickets>());
            fixture.Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
            return fixture;
        }
    }
}