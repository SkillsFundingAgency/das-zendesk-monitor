using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public class SharedTicket
    {
        public SharingReason Reason { get; }

        public TicketResponse Response { get; }

        public SharedTicket(SharingReason reason, TicketResponse response)
        {
            if (reason > SharingReason.Escalated)
                throw new ArgumentOutOfRangeException(nameof(reason), reason, "Undeclared SharingReason found.");

            Reason = reason;
            Response = response;
        }

        public T Switch<T>(Func<bool, T> solved, Func<bool, T> escalated)
        {
            return Reason switch
            {
                SharingReason.Solved => solved(true),
                SharingReason.Escalated => escalated(true),
                _ => throw new InvalidOperationException("Undeclared SharingReason found in Switch"),
            };
        }
    }
}