using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public static class IApiExtensions
    {
        private static SideLoads RequiredSideloads = new SideLoads
        {
            Organizations = true,
            Users = true,
        };

        public static Task<TicketResponse> PostTicket(this IApi api, Ticket ticket)
            => api.PostTicket(new TicketRequest { Ticket = ticket });

        public static Task PutTicket(this IApi api, Ticket ticket)
            => api.PutTicket(ticket.Id, new TicketRequest { Ticket = ticket });

        public static Task<TicketResponse> GetTicketWithRequiredSideloads(this IApi api, long id)
            => api.GetTicketWithSideloads(id, RequiredSideloads);

        public static async Task<Comment[]> GetTicketComments(this IApi api, Ticket ticket)
            => (await api.GetTicketComments(ticket.Id))?.Comments;
    }
}