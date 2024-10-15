using AutoMapper;
using SFA.DAS.Zendesk.Monitor.Zendesk.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SFA.DAS.Zendesk.Monitor
{
    public class TicketProfile : Profile
    {
        public TicketProfile()
        {
            CreateMap<Zendesk.Model.TicketResponse, Middleware.EventWrapper>()
                .ForMember(x => x.Ticket, x => x.MapFrom(p => p.Ticket))
                .ForPath(x => x.Ticket.Comments, x => x.MapFrom(p => p.Comments))
                .ForPath(x => x.Ticket.Requester, x => x.MapFrom(p => FindRequester(p)))
                .ForPath(x => x.Ticket.Organization, x => x.MapFrom(p => FindOrganisation(p)))
                ;

            CreateMap<Zendesk.Model.Ticket, Middleware.Model.Ticket>()
                .ForMember(dest => dest.Destination, src => src.MapFrom(p => FindDestination(p)))
                .ForMember(dest => dest.Comments, src => src.Ignore())
                .ForMember(dest => dest.Requester, src => src.Ignore())
                .ForMember(dest => dest.Organization, src => src.Ignore())
                .ForPath(dest => dest.Via, src => src.MapFrom(p => TranslateVia(p)))
                .ForMember(dest => dest.CustomFields,
                    src => src.MapFrom(t => CustomFieldsWithValues(t)));
            ;

            CreateMap<Zendesk.Model.Comment, Middleware.Model.Comments>();
            CreateMap<Zendesk.Model.Attachment, Middleware.Model.Attachment>()
                .ForMember(x => x.Filename, x => x.MapFrom(y => y.FileName))
                .ForMember(x => x.Url, x => x.MapFrom(y => y.ContentUrl))
                ;
            CreateMap<Zendesk.Model.Field, Middleware.Model.CustomField>();

            CreateMap<Zendesk.Model.User, Middleware.Model.User>()
                .ForMember(dest => dest.Email, src => src.NullSubstitute(""))
                .ForMember(dest => dest.Phone, src => src.NullSubstitute(""))
                ;

            CreateMap<Zendesk.Model.UserFields, Middleware.Model.UserFields>()
                .ForAllMembers(src => src.NullSubstitute(""))
                ;

            CreateMap<Zendesk.Model.Organization, Middleware.Model.Organisation>();
            CreateMap<Zendesk.Model.OrganizationFields, Middleware.Model.OrganizationFields>()
                .ForAllMembers(src => src.NullSubstitute(""))
                ;
        }

        private static string FindDestination(Ticket p) =>
            p.Tags
                ?.Find(t => t.StartsWith("middleware_destination_"))
                ?.Split("_").Last()
            ?? "itsm";

        private static Zendesk.Model.Organization? FindOrganisation(Zendesk.Model.TicketResponse response)
            => response.Organizations?.FirstOrDefault(x => x.Id == response.Ticket.OrganizationId);

        private static Zendesk.Model.User? FindRequester(Zendesk.Model.TicketResponse response)
            => response.Users?.FirstOrDefault(x => x.Id == response.Ticket.RequesterId);

        private static string TranslateVia(Zendesk.Model.Ticket y)
        {
            return (y.Via?.Channel, y.Via?.Source?.Rel) switch
            {
                ("voice", "voicemail") => "Voicemail",
                ("voice", _) => $"Phone call ({y.Via?.Source?.Rel})",
                ("email", _) => "Mail",
                ("twitter", _) => "Mail",
                ("api", _) => "Mail",
                ("chat", _) => "Chat",
                ("web", _) => "Web Form",
                _ => y.Via?.Channel.ToLower(),
            } ?? "";
        }

        private static IEnumerable<Zendesk.Model.Field> CustomFieldsWithValues(Zendesk.Model.Ticket t)
            => t.CustomFields.Where(f => f.Value != null);
    }
}