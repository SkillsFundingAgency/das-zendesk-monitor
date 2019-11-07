using AutoMapper;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor
{
    public class Watcher
    {
        private readonly ISharingTickets zendesk;
        private readonly Middleware.IApi middleware;
        private static readonly IMapper MapperConfig = CreateMapperConfig();

        public Watcher(ISharingTickets zendesk, Middleware.IApi middleware)
        {
            this.zendesk = zendesk ?? throw new ArgumentNullException(nameof(zendesk));
            this.middleware = middleware ?? throw new ArgumentNullException(nameof(middleware));
        }

        public Task<long[]> GetTicketsForSharing() => zendesk.GetTicketsForSharing();

        public async Task ShareTicket(long id)
        {
            var ticket = await zendesk.GetTicketForSharing(id);

            await ticket.IfSomeAsync(ShareTicket);
        }

        private async Task ShareTicket(Zendesk.Model.TicketResponse ticket)
        {
            await zendesk.MarkSharing(ticket.Ticket);

            var we2 = MapperConfig.Map<Middleware.Model.Ticket>(ticket.Ticket);
            we2.Comments = MapperConfig.Map<Middleware.Model.Comments[]>(ticket.Comments);
            we2.Requester = MapperConfig.Map<Middleware.Model.User>(ticket.Users?.FirstOrDefault(x => x.Id == ticket.Ticket.RequesterId));
            we2.Organization = MapperConfig.Map<Middleware.Model.Organisation>(ticket.Organizations?.FirstOrDefault(x => x.Id == ticket.Ticket.OrganizationId));

            await middleware.PostEvent(new Middleware.EventWrapper { Ticket = we2 });
            await zendesk.MarkShared(ticket.Ticket);
        }

        private static IMapper CreateMapperConfig()
        {
            return new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Zendesk.Model.Ticket, Middleware.Model.Ticket>()
                    .ForMember(x => x.CustomFields, x => x.Ignore());

                cfg.CreateMap<Zendesk.Model.User, Middleware.Model.User>()
                    .ForMember(x => x.UserFields, x => x.MapFrom(y => y.UserFields));

                cfg.CreateMap<Zendesk.Model.UserFields, Middleware.Model.UserFields>();

                cfg.CreateMap<Zendesk.Model.Organization, Middleware.Model.Organisation>()
                    .ForMember(x => x.OrganizationFields, x => x.MapFrom(y => y.OrganizationFields))
                    ;

                cfg.CreateMap<Zendesk.Model.OrganisationFields, Middleware.Model.OrganisationFields>();

                cfg.CreateMap<Zendesk.Model.Comment, Middleware.Model.Comments>();

            }).CreateMapper();
        }
    }
}