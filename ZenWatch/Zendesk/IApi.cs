using Refit;
using System.Threading.Tasks;

namespace ZenWatch.Zendesk
{
    public interface IApi
    {
        [Get("/tickets/{id}.json")]
        Task<TicketResponse> GetTicket(long id);
    }
}
