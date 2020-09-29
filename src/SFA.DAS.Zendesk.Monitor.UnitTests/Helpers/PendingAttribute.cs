using AutoFixture;
using AutoFixture.Dsl;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomisationFunc = System.Func<
    AutoFixture.IFixture,
    AutoFixture.Dsl.IPostprocessComposer<SFA.DAS.Zendesk.Monitor.Zendesk.Model.Ticket>,
    AutoFixture.Dsl.IPostprocessComposer<SFA.DAS.Zendesk.Monitor.Zendesk.Model.Ticket>>;

namespace SFA.DAS.Zendesk.Monitor.UnitTests.AutoFixtureCustomisation
{
    public abstract class Pending
    {
        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        public class Solved : PendingAttribute
        {
            public Solved(bool addTag = true)
                : base(nameof(Solved), addTag, SolvedTicketsDoNotHaveComments)
            {
            }

            // Agents do not use any macros when resolving tickets, so
            // there is no audited comment associated with the resolution.
            private const bool SolvedTicketsDoNotHaveComments = false;
        }

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        public class Escalated : PendingAttribute
        {
            public Escalated(bool addTag = true)
                : base(nameof(Escalated), addTag, EscalatedTicketsHaveAuditedComments)
            {
            }

            // When escalating tickets agents use a macro that adds a tagged
            // comment to the ticket.  The extra tag and comment occur in the
            // same audit event as the "pending" tag.
            private const bool EscalatedTicketsHaveAuditedComments = true;
        }

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        public class HandedOff : PendingAttribute
        {
            public HandedOff(bool addTag = true)
                : base(nameof(HandedOff), addTag, HandedOffTicketsHaveSimpleComments)
            {
            }

            // When handing tickets off to other systems, all comments are sent
            // to the middleware to be shown in the other system.
            private const bool HandedOffTicketsHaveSimpleComments = true;
        }

        public abstract class PendingAttribute : CustomizeAttribute, ICustomization
        {
            private readonly List<CustomisationFunc> customisations = new List<CustomisationFunc>();

            public PendingAttribute(string reason, bool addTag, bool addComment)
            {
                if (addTag)
                    customisations.Add((fixture, x) => AddTagCustomisation(fixture, x, reason));

                if (addComment)
                    customisations.Add((fixture, x) => AddTaggedCommentCustomisation(fixture, x));
            }

            private IPostprocessComposer<Ticket> AddTagCustomisation(
                IFixture _,
                IPostprocessComposer<Ticket> ticket, string reason)
            =>
                ticket
                    .Without(y => y.Tags)
                    .Do(y => y.Tags = new List<string> { $"pending_middleware_{reason.ToLower()}" });

            private IPostprocessComposer<Ticket> AddTaggedCommentCustomisation(
                IFixture fixture,
                IPostprocessComposer<Ticket> ticket)
            =>
                ticket
                    .Without(y => y.Id)
                    .Do(ticket =>
                    {
                        ticket.Id = fixture.Create<long>();

                        var comment = fixture.Create<AuditedComment>();
                        comment.Share();

                        var zendesk = fixture.Create<FakeZendeskApi>();
                        zendesk.AddComments(ticket, new[] { comment });
                    });

            public override ICustomization GetCustomization(ParameterInfo parameter)
            {
                if (parameter.ParameterType != typeof(Ticket))
                    throw new InvalidOperationException(
                        $"`{parameter.ParameterType.Name}` is not a valid type for [Pending].");
                return this;
            }

            public void Customize(IFixture fixture)
                => fixture.Customize(ApplyAllCustomisations(fixture));

            private Func<ICustomizationComposer<Ticket>, ISpecimenBuilder> ApplyAllCustomisations(IFixture fixture)
                => composer => ApplyAllCustomisations(fixture, composer);

            private IPostprocessComposer<Ticket> ApplyAllCustomisations(
                IFixture fixture,
                ICustomizationComposer<Ticket> composer)
            =>
                customisations.Aggregate(
                    (IPostprocessComposer<Ticket>)composer,
                    (c, f) => f(fixture, c));
        }
    }
}