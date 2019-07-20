using System.Threading.Tasks;

namespace ZenWatch.Zendesk
{
    public interface ISharingTickets
    {
        Task<Ticket[]> GetTicketsForSharing();

        void MarkSharing(Ticket t);

        void MarkShared(Ticket t);
    }
}