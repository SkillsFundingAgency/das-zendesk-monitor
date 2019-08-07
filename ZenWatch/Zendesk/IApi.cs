using RestEase;
using System.Threading.Tasks;

namespace ZenWatch.Zendesk
{
    public interface IApi
    {
        [Get("/tickets/{id}.json")]
        Task<TicketResponse> GetTicket(long id);

        [Get("/search.json?query={query}")]
        Task<SearchResponse> SearchTickets(string query);

        [Post("/tickets.json")]
        Task<TicketResponse> PostTicket([Body] Empty ticket);

        [Put("/tickets/{id}.json")]
        Task PutTicket(long id, [Body] Empty ticket);
    }
}