using LanguageExt;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public interface ISharingTickets
    {
        Task<long[]> GetTicketsForSharing();

        Task<Option<SharedTicket>> GetTicketForSharing(long id);

        Task MarkSharing(SharedTicket share);

        Task MarkShared(SharedTicket share);
    }
}