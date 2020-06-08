using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public static class IApiExtensions
    {
        private static readonly SideLoads RequiredSideloads = new SideLoads
        {
            Organizations = true,
            Users = true,
            Audits = true,
        };

        internal static Task<TicketResponse> PostTicket(this IApi api, Ticket ticket)
            => api.PostTicket(new TicketRequest { Ticket = ticket });

        internal static Task UpdateTickets(this IApi api, Ticket ticket)
            => api.UpdateTicket(ticket.Id, new TicketRequest { Ticket = ticket });

        public static async Task ModifyTags(
            this IApi api,
            Ticket ticket,
            string[]? additions = null,
            string[]? removals = null)
        {
            if (api == null) throw new ArgumentNullException(nameof(api));
            if (ticket == null) throw new ArgumentNullException(nameof(ticket));

            var ticketResponse = await api.GetTicket(ticket.Id);

            var update = new SafeModifyTags(ticketResponse.Ticket);
            update.Add(additions ?? Array.Empty<string>());
            update.Remove(removals ?? Array.Empty<string>());

            await api.UpdateTags(ticket.Id, update);
        }

        internal static Task<TicketResponse> GetTicketWithRequiredSideloads(this IApi api, long id)
            => api.GetTicketWithSideloads(id, RequiredSideloads);

        internal static async Task<Comment[]> GetTicketComments(this IApi api, Ticket ticket)
            => (await api.GetTicketComments(ticket.Id))?.Comments ?? Array.Empty<Comment>();

        internal static async Task<Audit[]> GetTicketAudits(this IApi api, Ticket ticket)
            => (await api.GetTicketAudits(ticket.Id))?.Audits ?? Array.Empty<Audit>();
    }
}