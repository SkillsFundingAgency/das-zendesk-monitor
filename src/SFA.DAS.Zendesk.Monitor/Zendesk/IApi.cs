using RestEase;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public interface IApi
    {
        [Get("/tickets/{id}.json")]
        Task<TicketResponse> GetTicket([Path] long id);

        [Get("/tickets/{id}.json")]
        Task<TicketResponse> GetTicketWithSideloads([Path] long id, SideLoads include);

        [Get("/search.json")]
        Task<SearchResponse> SearchTickets([Query] string query);

        [Post("/tickets.json")]
        Task<TicketResponse> PostTicket([Body] TicketRequest ticket);

        [Put("/tickets/{id}.json")]
        Task PutTicket([Path] long id, [Body] TicketRequest ticket);

        [Get("/tickets/{id}/comments.json")]
        Task<CommentResponse> GetTicketComments([Path] long id);
    }

    public class SideLoads
    {
        public bool Users { get; set; }

        public bool Organizations { get; set; }

        public override string ToString() => string.Join(",", Include());

        private IEnumerable<string> Include()
        {
            if (Users) yield return "users";
            if (Organizations) yield return "organizations";
        }
    }
}