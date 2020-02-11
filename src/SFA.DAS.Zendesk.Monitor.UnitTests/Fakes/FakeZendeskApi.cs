using RestEase;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class FakeZendeskApi : IApi
    {
        public List<Ticket> Tickets { get; } = new List<Ticket>();

        public List<User> Users { get; set; } = new List<User>();

        public List<Organization> Organizations { get; set; } = new List<Organization>();

        public Dictionary<long, List<Comment>> Comments { get; } = new Dictionary<long, List<Comment>>();

        public Dictionary<long, List<Audit>> Audits { get; } = new Dictionary<long, List<Audit>>();

        public Task<SearchResponse> SearchTickets(string query)
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

        public Task<TicketResponse> GetTicketWithSideloads(long id, SideLoads include)
        {
            var ticket = Tickets.First(x => x.Id == id);
            var response = new TicketResponse { Ticket = ticket };

            response.Users = SideLoad(Users, x => x.Id == response.Ticket.RequesterId, include);
            response.Organizations = SideLoad(Organizations, x => x.Id == response.Ticket.OrganizationId, include);

            return Task.FromResult(response);
        }

        private T[] SideLoad<T>(List<T> resources, Func<T, bool> p, SideLoads include)
        {
            var name = $"{typeof(T).Name}s".ToLower();
            if (!include.ToString().Contains(name)) return new T[] { };
            return resources.Where(p).ToArray();
        }

        public Task<CommentResponse> GetTicketComments(long id)
        {
            var comments = TicketComments(id).ToArray();
            var response = new CommentResponse { Comments = comments };
            return Task.FromResult(response);
        }

        public Task<AuditResponse> GetTicketAudits(long id)
        {
            var audits = TicketAudits(id).ToArray();
            var response = new AuditResponse { Audits = audits };
            return Task.FromResult(response);
        }

        public Task<TicketResponse> PostTicket([Body] TicketRequest ticket)
            => Task.FromResult<TicketResponse>(null);

        public Task UpdateTags([Path] long id, [Body] SafeModifyTags update)
        {
            Tickets.First(x => x.Id == id).Tags = update.Tags;
            return Task.CompletedTask;
        }

        public Task PutTicket([Path] long id, [Body] TicketRequest ticket)
            => Task.CompletedTask;

        internal void AddComments(Ticket ticket, Comment[] comments)
            => TicketComments(ticket.Id).AddRange(comments);

        internal void AddComments(Ticket ticket, AuditedComment[] comments)
        {
            TicketComments(ticket.Id).AddRange(comments);
            TicketAudits(ticket.Id).AddRange(comments.Select(a => a.AsAudit));
        }

        private List<Comment> TicketComments(long id)
            => Comments.GetOrAdd(id, () => new List<Comment>());

        private List<Audit> TicketAudits(long id)
            => Audits.GetOrAdd(id, () => new List<Audit>());
    }
}