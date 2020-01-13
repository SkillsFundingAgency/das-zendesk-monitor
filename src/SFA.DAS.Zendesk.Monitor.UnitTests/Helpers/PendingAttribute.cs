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
            public Solved(bool addTag = true, bool addComment = true)
                : base(nameof(Solved), addTag, addComment)
            {
            }
        }

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        public class Escalated : PendingAttribute
        {
            public Escalated(bool addTag = true, bool addComment = true)
                : base(nameof(Escalated), addTag, addComment)
            {
            }
        }

        public abstract class PendingAttribute : CustomizeAttribute, ICustomization
        {
            private readonly List<CustomisationFunc> customisations = new List<CustomisationFunc>();

            public PendingAttribute(string reason, bool addTag = true, bool addComment = true)
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
                        //zendesk.Tickets.Add(ticket);
                        zendesk.AddComments(ticket, new[] { comment });
                        zendesk.AddAudits(ticket, new[] { comment.AsAudit });
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