using RestEase;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Collections.Generic;
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
        Task UpdateTags([Path] long id, [Body] SafeModifyTags update);

        [Put("/tickets/{id}.json")]
        Task UpdateTicket([Path] long id, [Body] TicketRequest ticket);

        [Get("/tickets/{id}/comments.json")]
        Task<CommentResponse> GetTicketComments([Path] long id);
        
        [Get("/tickets/{id}/audits.json")]
        Task<AuditResponse> GetTicketAudits([Path] long id);
    }

    public class SideLoads
    {
        public bool Users { get; set; }

        public bool Organizations { get; set; }
        
        public bool Audits { get; set; }

        public override string ToString() => string.Join(",", Include());

        private IEnumerable<string> Include()
        {
            if (Users) yield return "users";
            if (Organizations) yield return "organizations";
            if (Audits) yield return "audits";
        }
    }
}