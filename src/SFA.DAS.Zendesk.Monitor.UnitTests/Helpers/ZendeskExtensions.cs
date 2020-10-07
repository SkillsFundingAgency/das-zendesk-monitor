namespace SFA.DAS.Zendesk.Monitor.UnitTests.Helpers
{
    public static class ZendeskExtensions
    {
        public static AuditedComment[] ShareAll(this AuditedComment[] comments)
        {
            foreach (var c in comments) 
                c.Escalate();
            return comments;
        }
    }
}
