using AutoMapper;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Linq;

namespace SFA.DAS.Zendesk.Monitor
{
    public class TicketProfile : Profile
    {
        public TicketProfile()
        {
            CreateMap<Zendesk.Model.TicketResponse, Middleware.EventWrapper>()
                .ForMember(x => x.Ticket, x => x.MapFrom(y => y.Ticket))
                .ForPath(x => x.Ticket.Comments, x => x.MapFrom(y => y.Comments))
                .ForPath(x => x.Ticket.Requester, x => x.MapFrom(y => FindRequester(y)))
                .ForPath(x => x.Ticket.Organization, x => x.MapFrom(y => FindOrganisation(y)))
                ;

            CreateMap<Zendesk.Model.Ticket, Middleware.Model.Ticket>()
                .ForMember(x => x.Comments, x => x.Ignore())
                .ForMember(x => x.Requester, x => x.Ignore())
                .ForMember(x => x.Organization, x => x.Ignore())
                .ForPath(x => x.Via, x => x.MapFrom(y => TranslateVia(y)))
                ;

            CreateMap<Zendesk.Model.Comment, Middleware.Model.Comments>();
            CreateMap<Zendesk.Model.Field, Middleware.Model.CustomField>();
            CreateMap<Zendesk.Model.User, Middleware.Model.User>();
            CreateMap<Zendesk.Model.Organization, Middleware.Model.Organisation>();
            CreateMap<Zendesk.Model.UserFields, Middleware.Model.UserFields>();
            CreateMap<Zendesk.Model.OrganizationFields, Middleware.Model.OrganizationFields>();
        }

        private Zendesk.Model.Organization FindOrganisation(Zendesk.Model.TicketResponse response)
            => response.Organizations?.FirstOrDefault(x => x.Id == response.Ticket.OrganizationId);

        private Zendesk.Model.User FindRequester(Zendesk.Model.TicketResponse response)
            => response.Users?.FirstOrDefault(x => x.Id == response.Ticket.RequesterId);

        private string TranslateVia(Ticket y)
        {
            switch (y.Via?.Channel)
            {
                case "voice": return $"Phone call ({y.Via?.Source?.Rel})";
                case "email": return "Mail";
                case "chat": return "Chat";
                case "web": return "Web Form";
                
                default: return y.Via?.Channel.ToLower();
            }
        }
    }
}