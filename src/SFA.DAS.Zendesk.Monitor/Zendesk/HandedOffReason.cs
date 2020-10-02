﻿using SFA.DAS.Zendesk.Monitor.Zendesk.Model;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public partial class SharingReason
    {
        private class HandedOffReason : SharingReason
        {
            public HandedOffReason(string name, int value) : base(name, value)
            {
            }

            public override TicketResponse AddCommentsToResponse(TicketResponse response, Comment[] comments, Audit[] audits)
            {
                response.Comments = comments;
                return response;
            }
        }
    }
}