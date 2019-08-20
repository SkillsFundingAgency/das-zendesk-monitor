using RestEase;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class FakeZendeskApi : IApi
    {
        public List<Ticket> Tickets { get; } = new List<Ticket>();

        public List<User> Users { get; set; } = new List<User>();

        public List<Organisation> Organisations { get; set; } = new List<Organisation>();

        public Dictionary<long, List<Comment>> Comments { get; } = new Dictionary<long, List<Comment>>();

        public Task<SearchResponse> SearchTickets([Query] string query)
        {
            var response = new SearchResponse { Results = Tickets.ToArray() };
            return Task.FromResult(response);
        }

        public Task<TicketResponse> GetTicket([Path] long id)
        {
            var ticket = Tickets.First(x => x.Id == id);
            var response = new TicketResponse { Ticket = ticket };
            return Task.FromResult(response);
        }

        public Task<TicketResponse> GetTicketWithSideloads([Path] long id, [Query(name: "include")] params string[] sideLoad)
        {
            var ticket = Tickets.First(x => x.Id == id);
            var response = new TicketResponse { Ticket = ticket };

            var users = new List<User>();
            if (sideLoad.Contains("users"))
                users.AddRange(Users.Where(x => x.Id == response.Ticket.RequesterId));
            response.Users = users.ToArray();

            var orgs = new List<Organisation>();
            if (sideLoad.Contains("organizations"))
                orgs.AddRange(Organisations.Where(x => x.Id == response.Ticket.OrganizationId));
            response.Organizations = orgs.ToArray();

            return Task.FromResult(response);
        }

        public Task<CommentResponse> GetTicketComments(long id)
        {
            var comments = TicketComments(id).ToArray();
            var response = new CommentResponse { Comments = comments };
            return Task.FromResult(response);
        }

        public Task<TicketResponse> PostTicket([Body] TicketRequest ticket) => Task.FromResult<TicketResponse>(null);

        public Task PutTicket([Path] long id, [Body] TicketRequest ticket) => Task.CompletedTask;

        internal void AddComments(Ticket ticket, Comment[] comments)
        {
            TicketComments(ticket.Id).AddRange(comments);
        }

        private List<Comment> TicketComments(long id) => Comments.GetOrAdd(id, () => new List<Comment>());
    }

    /*
     * Watcher marks ticket as sharing before sending to middleware
     * Watcher marks ticket as shared after successfully sending to middleware
     * Watcher leaves ticket as sharing after unsuccessfully sending to middleware
     *
     */
}