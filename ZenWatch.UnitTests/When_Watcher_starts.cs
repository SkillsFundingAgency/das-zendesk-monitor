using AutoFixture.Xunit2;
using FizzWare.NBuilder;
using FluentAssertions;
using NSubstitute;
using RestEase;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ZenWatch.Zendesk;

namespace ZenWatch.UnitTests
{
    public abstract class FakeSharingTickets : ISharingTickets
    {
        public List<Ticket> Tickets { get; set; } = new List<Ticket>();

        public Task<Ticket> GetTicketForSharing(long id) => Task.FromResult(Tickets.FirstOrDefault(x => x.Id == id));

        public Task<long[]> GetTicketsForSharing() => Task.FromResult(Tickets.Select(x => x.Id).ToArray());

        public abstract Task MarkShared(Ticket t);

        public abstract Task MarkSharing(Ticket t);
    }

    public class FakeZendeskApi : Zendesk.IApi
    {
        public List<Ticket> Tickets { get; set; } = new List<Ticket>();

        public Task<TicketResponse> GetTicket([Path] long id) => Task.FromResult(new TicketResponse { Ticket = Tickets.First(x => x.Id == id) });

        public Task<TicketResponse> PostTicket([Body] Empty ticket) => Task.FromResult<TicketResponse>(null);

        public Task PutTicket([Path] long id, [Body] Empty ticket) => Task.CompletedTask;

        public Task<SearchResponse> SearchTickets([Query] string query) => Task.FromResult(new SearchResponse { Results = Tickets.ToArray() });
    }

    public class When_there_is_one_ticket_to_be_shared
    {
        private readonly Middleware.IApi middleware = Substitute.For<Middleware.IApi>();
        private readonly FakeSharingTickets zendesk = Substitute.ForPartsOf<FakeSharingTickets>();

        [Theory, AutoMockData]
        public async Task Watcher_marks_ticket_as_sharing_before_sending_to_middleware([Frozen(Matching.ImplementedInterfaces)] FakeSharingTickets zendesk, Watcher sut, Zendesk.Ticket ticket)
        {
            zendesk.Tickets.Add(ticket);

            await sut.Watch();

            await zendesk.Received().MarkSharing(ticket);
        }

        [Fact]
        public async Task Then_watcher_sends_that_ticket_to_middleware()
        {
            var sut = new Watcher(zendesk, middleware);
            var ticket = Builder<Zendesk.Ticket>.CreateNew().Build();
            zendesk.Tickets.Add(ticket);

            await sut.Watch();

            await middleware.Received().PostEvent(Verify.That<Middleware.EventWrapper>(x => x.Ticket.Should().BeEquivalentTo(ticket)));
        }

        [Fact]
        public async Task Then_watcher_marks_ticket_as_shared()
        {
            var sut = new Watcher(zendesk, middleware);
            var ticket = Builder<Zendesk.Ticket>.CreateNew().Build();
            zendesk.Tickets.Add(ticket);

            await sut.Watch();

            await zendesk.Received().MarkShared(ticket);
        }

        [Theory, AutoMockData]
        public async Task Ticket_returned_from_search_but_not_actually_pending([Frozen(Matching.ImplementedInterfaces)] FakeZendeskApi zendesk, SharingTickets sut, Zendesk.Ticket ticket)
        {
            zendesk.Tickets.Add(ticket);
            ticket.Tags.Clear();

            var found = await sut.GetTicketsForSharing();

            found.Should().BeEmpty();
        }
    }

    /*
     * Watcher marks ticket as sharing before sending to middleware
     * Watcher marks ticket as shared after successfully sending to middleware
     * Watcher leaves ticket as sharing after unsuccessfully sending to middleware
     *
     */
}

namespace NSubstitute
{
    using AutoFixture;
    using AutoFixture.AutoNSubstitute;
    using AutoFixture.Xunit2;
    using FluentAssertions.Execution;
    using NSubstitute.Core;
    using NSubstitute.Core.Arguments;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Verify
    {
        private readonly static ArgumentFormatter DefaultArgumentFormatter = new ArgumentFormatter();

        /// <summary>
        /// Verify the NSubstitute call using FluentAssertions.
        /// </summary>
        /// <example>
        /// <code>
        /// var sub = Substitute.For&lt;ISomeInterface&gt;();
        /// sub.InterestingMethod("Hello hello");
        ///
        /// sub.Received().InterestingMethod(Verify.That&lt;string&gt;(s => s.Should().StartWith("hello").And.EndWith("goodbye")));
        /// </code>
        /// Results in the failure message:
        /// <code>
        /// Expected to receive a call matching:
        ///     SomeMethod("
        /// Expected string to start with
        /// "hello", but
        /// "Hello hello" differs near "Hel" (index 0).
        /// Expected string
        /// "Hello hello" to end with
        /// "goodbye".")
        /// Actually received no matching calls.
        /// Received 1 non-matching call(non-matching arguments indicated with '*' characters):
        ///     SomeMethod(*"Hello hello"*)
        /// </code>
        /// </example>
        /// <typeparam name="T">Type of argument to verify.</typeparam>
        /// <param name="action">Action in which to perform FluentAssertions verifications.</param>
        /// <returns></returns>
        public static T That<T>(Action<T> action)
            => ArgumentMatcher.Enqueue(new AssertionMatcher<T>(action));

        //=> ArgumentMatcher.Enqueue(new GenericToNonGenericMatcherProxyWithDescribe<T>(new AssertionMatcher<T>(action)));

        private class AssertionMatcher<T> : IArgumentMatcher<T>, IDescribeNonMatches
        {
            private readonly Action<T> assertion;
            private string allFailures = "";

            public AssertionMatcher(Action<T> assertion)
                => this.assertion = assertion;

            public bool IsSatisfiedBy(T argument)
            {
                using (var scope = new AssertionScope())
                {
                    try
                    {
                        assertion((T)argument);
                    }
                    catch (Exception exception)
                    {
                        var f = scope.Discard();
                        allFailures = f.Any() ? AggregateFailures(f) : exception.Message;
                        return false;
                    }

                    var failures = scope.Discard().ToList();

                    if (failures.Count == 0) return true;

                    allFailures = String.Join("\n", failures);
                    //allFailures = AggregateFailures(failures);

                    return false;
                }
            }

            private string AggregateFailures(IEnumerable<string> discard)
                => discard.Aggregate(allFailures, (a, b) => a + "\n" + b);

            public override string ToString()
                => DefaultArgumentFormatter.Format(allFailures, false);

            public string DescribeFor(object argument) => ToString();
        }
    }

    public class AutoMockDataAttribute : AutoDataAttribute
    {
        public AutoMockDataAttribute()
            : base(() => new Fixture().Customize(new AutoNSubstituteCustomization()))
        {
        }
    }
}