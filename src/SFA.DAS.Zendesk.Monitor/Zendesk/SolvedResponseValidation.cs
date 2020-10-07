using LanguageExt;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using static LanguageExt.Prelude;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    internal class SolvedResponseValidation : ISharingValidation
    {
        public SharingReason Reason => SharingReason.Solved;

        public Option<SharedTicket> TryShareTicket(TicketResponse response)
        {
            return response.IsSharedBecause(Reason)
                ? Some(new SharedTicket(Reason, response.WithoutComments()))
                : None;
        }
    }
}