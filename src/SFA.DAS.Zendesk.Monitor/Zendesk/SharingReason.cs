namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public enum SharingReason
    {
        Solved,
        Escalated,
    }

    public static class SharingReasonExtensions
    {
        public static string AsTag(this SharingReason reason)
            => $"middleware_{reason}".ToLower();
    }
}