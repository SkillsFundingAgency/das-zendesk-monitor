using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public interface ISharingTickets
    {
        Task<long[]> GetTicketsForSharing();

        Task<Ticket> GetTicketForSharing(long id);

        Task MarkSharing(Ticket t);

        Task MarkShared(Ticket t);
    }
}