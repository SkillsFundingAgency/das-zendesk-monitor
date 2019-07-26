using System.Threading.Tasks;

namespace ZenWatch.Zendesk
{
    public interface ISharingTickets
    {
        Task<Ticket[]> GetTicketsForSharing();

        Task MarkSharing(Ticket t);

        Task MarkShared(Ticket t);
    }
}