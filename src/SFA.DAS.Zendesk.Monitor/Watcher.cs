﻿using AutoMapper;
using SFA.DAS.Zendesk.Monitor.Zendesk;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor
{
    public class Watcher
    {
        private static readonly IMapper MapperConfig =
            new MapperConfiguration(c => c.AddProfile<TicketProfile>())
                .CreateMapper();

        private readonly ISharingTickets zendesk;
        private readonly Middleware.IApi middleware;

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

            var wrap = MapperConfig.Map<Middleware.EventWrapper>(ticket);
            await middleware.SolveTicket(wrap);

            await zendesk.MarkShared(ticket.Ticket);
        }
    }
}