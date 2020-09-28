using Ardalis.SmartEnum;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public sealed class SharingReason : SmartEnum<SharingReason>
    {
        public static readonly SharingReason Solved = new SharingReason(nameof(Solved), 0);
        public static readonly SharingReason Escalated = new SharingReason(nameof(Escalated), 1);

        private SharingReason(string name, int value) : base(name, value)
        {
        }

        public string AsTag() => $"middleware_{Name}".ToLower();
    }
}