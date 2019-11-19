using AutoFixture;
using AutoFixture.Xunit2;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SFA.DAS.Zendesk.Monitor.UnitTests.AutoFixture
{
    public enum As
    {
        Solved,
        Escalated,
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class PendingAttribute : CustomizeAttribute
    {
        private As solved;

        public PendingAttribute(As solved)
        {
            this.solved = solved;
        }

        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            if (parameter.ParameterType != typeof(Ticket))
                throw new InvalidOperationException($"`{parameter.ParameterType.Name}` is not a valid type for [Pending].");
            return new PendingTicketCustomisation(solved);
        }

        private class PendingTicketCustomisation : ICustomization
        {
            private string reason;

            public PendingTicketCustomisation(As reason)
            {
                this.reason = reason.ToString().ToLower();
            }

            public void Customize(IFixture fixture)
            {
                fixture.Customize<Ticket>(x => x
                    .Without(y => y.Tags)
                    .Do(y => y.Tags = new List<string> { $"pending_middleware_{reason}" }));
            }
        }
    }
}