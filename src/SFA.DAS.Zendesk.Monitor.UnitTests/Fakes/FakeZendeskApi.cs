﻿using RestEase;
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

        public Task<TicketResponse> GetTicketWithSideloads(long id, params string[] include)
        {
            var ticket = Tickets.First(x => x.Id == id);
            var response = new TicketResponse { Ticket = ticket };

            response.Users = SideLoad(Users, x => x.Id == response.Ticket.RequesterId, include);
            response.Organizations = SideLoad(Organizations, x => x.Id == response.Ticket.OrganizationId, include);

            return Task.FromResult(response);
        }

        private T[] SideLoad<T>(List<T> resources, Func<T, bool> p, string[] include)
        {
            var name = $"{typeof(T).Name}s".ToLower();
            if (!include.Contains(name)) return new T[] { };
            return resources.Where(p).ToArray();
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