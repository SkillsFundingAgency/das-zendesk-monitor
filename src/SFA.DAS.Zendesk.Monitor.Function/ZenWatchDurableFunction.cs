using FluentValidation;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZenWatchFunction
{
    internal class NotifyTicket
    {
        public long Id { get; set; }
    }

    internal class NotifyTicketValidator : AbstractValidator<NotifyTicket>
    {
        public NotifyTicketValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class WatcherOrchestration
    {
        private static readonly RetryOptions retry = new RetryOptions(TimeSpan.FromSeconds(1), 5);

        [FunctionName("NotifyTicket")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage request,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            var input = await request.GetJsonBody<NotifyTicket, NotifyTicketValidator>();

            return await input.Match(
                valid => StartNotifyTicket(valid),
                invalid => Task.FromResult(request.BadRequest(invalid)));

            async Task<HttpResponseMessage> StartNotifyTicket(NotifyTicket ticket)
            {
                var instanceId = await starter.StartNewAsync(nameof(ShareListedTickets), new[] { ticket.Id });
                return starter.CreateCheckStatusResponse(request, instanceId);
            }
        }

        [FunctionName(nameof(ShareAllTickets))]
        public static async Task ShareAllTickets([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var tickets = await context.CallActivityAsync<long[]>(nameof(DurableWatcher.SearchTickets), null);

            foreach (var ticket in tickets)
                await context.CallActivityWithRetryAsync(nameof(DurableWatcher.ShareTicket), retry, ticket);
        }

        [FunctionName(nameof(ShareListedTickets))]
        public static async Task ShareListedTickets([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var tickets = context.GetInput<long[]>();

            foreach (var ticket in tickets)
                await context.CallActivityWithRetryAsync(nameof(DurableWatcher.ShareTicket), retry, ticket);
        }
    }
}