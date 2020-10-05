using LanguageExt;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    internal interface ISharingValidation
    {
        SharingReason Reason { get; }

        Option<SharedTicket> TryShareTicket(TicketResponse response);
    }
}