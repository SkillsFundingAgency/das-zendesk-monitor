using RestEase;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public interface IApi
    {
        [Get("/tickets/{id}.json")]
        Task<TicketResponse> GetTicket([Path] long id);

        [Get("/tickets/{id}.json")]
        Task<TicketResponse> GetTicket([Path] long id, [Query(name: "include")] params string[] sideLoad);

        [Get("/search.json")]
        Task<SearchResponse> SearchTickets([Query] string query);

        [Post("/tickets.json")]
        Task<TicketResponse> PostTicket([Body] Empty ticket);

        [Put("/tickets/{id}.json")]
        Task PutTicket([Path] long id, [Body] Empty ticket);

        [Get("/tickets/{id}/comments.json")]
        Task<CommentResponse> GetTicketComments([Path] long id);
    }
}