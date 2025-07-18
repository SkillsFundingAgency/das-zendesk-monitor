using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        private readonly ILogger<Watcher> logger;

        public Watcher(ISharingTickets zendesk, Middleware.IApi middleware)
            : this(zendesk, middleware, NullLogger<Watcher>.Instance)
        {
        }

        public Watcher(ISharingTickets zendesk, Middleware.IApi middleware, ILogger<Watcher> logger)
        {
            this.zendesk = zendesk ?? throw new ArgumentNullException(nameof(zendesk));
            this.middleware = middleware ?? throw new ArgumentNullException(nameof(middleware));
            this.logger = logger;
        }

        public Task<long[]> GetTicketsForSharing() => zendesk.GetTicketsForSharing();

        public async Task ShareTicket(long id)
        {
            var ticket = await zendesk.GetTicketForSharing(id);
            
            await ticket.IfSomeAsync(ShareTicket);  
        }

        private async Task ShareTicket(SharedTicket share)
        {
            logger?.LogInformation($"Sharing {share.Reason} ticket {share.Id}");

            await zendesk.MarkSharing(share);

            var wrap = MapperConfig.Map<Middleware.EventWrapper>(share.Response);
            string json = string.Empty;

            try
            {
                json = wrap.ToString();

                await share.Switch(
                    solved => middleware.SolveTicket(wrap),
                    handedOff => middleware.HandOffTicket(wrap),
                    escalated => middleware.EscalateTicket(wrap)
                );

                await zendesk.MarkShared(share);

                logger?.LogInformation($"Shared {share.Reason} ticket {share.Id}");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"Error sharing {share.Reason} ticket {share.Id}. Payload: {json}");
                throw;
            }
        }
    }
}