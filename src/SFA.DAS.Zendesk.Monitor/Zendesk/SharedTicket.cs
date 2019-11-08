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
                throw new ArgumentOutOfRangeException("Undeclared SharingReason found.");

            Reason = reason;
            Response = response;
        }

        public T Switch<T>(Func<bool, T> solved, Func<bool, T> escalated)
        {
            switch(Reason)
            {
                case SharingReason.Solved:
                    return solved(true);

                case SharingReason.Escalated:
                    return escalated(true);

                default:
                    throw new InvalidOperationException("Undeclared SharingReason found in Switch");
            }
        }
    }
}