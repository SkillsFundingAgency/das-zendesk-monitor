namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    internal enum SharingReason
    {
        Solved,
        Escalated,
        HandedOff,
    }

    internal static class SharingReasonExtensions
    {
        internal static string AsTag(this SharingReason reason) =>
            $"middleware_{reason}".ToLower();
    }
}