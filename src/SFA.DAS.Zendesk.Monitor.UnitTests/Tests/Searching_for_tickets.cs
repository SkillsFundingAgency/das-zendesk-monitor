using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class Searching_for_tickets
    {
        public static object[] Tags = new[]
        {
            new[] { "pending_middleware" },
            new[] { "sending_middleware" },
        };

        [Theory]
        [MemberAutoDomainData(nameof(Tags))]
        public async Task Queries_for_tag(string tagName, [Frozen] IApi zendesk, Watcher sut)
        {
            await sut.GetTicketsForSharing();

            await zendesk.Received().SearchTickets(
                Verify.That<string>(x => x.Should().Contain($"tags:{tagName}")));
        }

        [Theory]
        [MemberAutoDomainData(nameof(Tags))]
        public async Task Returns_all_tickets_with_tag(string tagName, [Frozen] IApi zendesk, Watcher sut, Ticket ticket)
        {
            ticket.Tags = new List<string> { tagName };
            zendesk.SearchTickets(Arg.Any<string>())
                .Returns(SearchResponse.Create(ticket));

            var result = await sut.GetTicketsForSharing();

            result.Should().Contain(ticket.Id);
        }

        [Theory, AutoDomainData]
        public async Task Filters_out_tickets_returned_by_api_that_do_not_have_tag([Frozen] IApi zendesk, Watcher sut, Ticket ticket)
        {
            ticket.Tags.Clear();
            zendesk.SearchTickets(Arg.Any<string>())
                .Returns(SearchResponse.Create(ticket));

            var result = await sut.GetTicketsForSharing();

            result.Should().BeEmpty();
        }

        [Theory, AutoDomainData]
        public async Task Handles_no_results([Frozen] IApi zendesk, Watcher sut)
        {
            zendesk.SearchTickets(Arg.Any<string>())
                .Returns(SearchResponse.Create());

            var result = await sut.GetTicketsForSharing();

            result.Should().BeEmpty();
        }

        [Theory, AutoDomainData]
        public async Task Handles_null_results([Frozen] IApi zendesk, Watcher sut)
        {
            zendesk.SearchTickets(Arg.Any<string>())
                .Returns((SearchResponse)null);

            var result = await sut.GetTicketsForSharing();

            result.Should().BeEmpty();
        }

        [Theory, AutoDomainData]
        public async Task Handles_results_with_null_tickets([Frozen] IApi zendesk, Watcher sut)
        {
            zendesk.SearchTickets(Arg.Any<string>())
                .Returns(new SearchResponse { });

            var result = await sut.GetTicketsForSharing();

            result.Should().BeEmpty();
        }

        public class AutoDomainDataAttribute : AutoDataAttribute
        {
            public AutoDomainDataAttribute()
                : base(() => Customise())
            {
            }

            private static IFixture Customise()
            {
                var fixture = new Fixture();
                fixture.Register<ISharingTickets>(() => fixture.Create<SharingTickets>());
                fixture.Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
                return fixture;
            }
        }
 
        public class MemberAutoDomainDataAttribute : MemberAutoDataAttribute
        {
            public MemberAutoDomainDataAttribute(string memberName, params object[] parameters)
                : base(new AutoDomainDataAttribute(), memberName, parameters)
            {
            }
        }
    }
}