using Ardalis.SmartEnum;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public abstract partial class SharingReason : SmartEnum<SharingReason>
    {
        public static readonly SharingReason Solved = new SolvedReason(nameof(Solved), 0);
        public static readonly SharingReason Escalated = new EscalatedReason(nameof(Escalated), 1);
        public static readonly SharingReason HandedOff = new HandedOffReason(nameof(HandedOff), 2);

        private SharingReason(string name, int value) : base(name, value)
        {
        }

        public string AsTag() => $"middleware_{Name}".ToLower();

        public abstract TicketResponse AddCommentsToResponse(TicketResponse response, Comment[] comments, Audit[] audits);
    }
}