using RestEase;
using System.Threading.Tasks;

namespace ZenWatch.Zendesk
{
    public interface IApi
    {
        [Get("/tickets/{id}.json")]
        Task<TicketResponse> GetTicket([Path] long id);

        [Get("/search.json")]
        Task<SearchResponse> SearchTickets([Query] string query);

        [Post("/tickets.json")]
        Task<TicketResponse> PostTicket([Body] Empty ticket);

        [Put("/tickets/{id}.json")]
        Task PutTicket([Path] long id, [Body] Empty ticket);
    }
}