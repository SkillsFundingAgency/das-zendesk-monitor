using FluentValidation;
using Microsoft.AspNetCore.Http;
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
        private static readonly string WatcherInstance = "{8B2772F1-0A07-4D64-BEBE-1402520C0BD0}";
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

        [FunctionName("BackgroundTaskEntryPoint")]
        public static Task Run(
            [TimerTrigger("%MonitorCronSetting%")] TimerInfo timer,
            [OrchestrationClient] DurableOrchestrationClient starter,
            ILogger log)
        {
            return GetSingleInstance(starter, log);
        }

        private static async Task<DurableOrchestrationStatus> GetSingleInstance(DurableOrchestrationClient starter, ILogger log)
        {
            var instance = await starter.GetStatusAsync(WatcherInstance);

            if (instance?.OrchestrationIsRunning() != true)
            {
                log.LogInformation("Starting Watcher orchestration");
                await starter.StartNewAsync(nameof(ShareAllTickets), WatcherInstance, null);
            }
            else
            {
                log.LogWarning("Watcher orchestration is already running");
            }

            return instance;
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