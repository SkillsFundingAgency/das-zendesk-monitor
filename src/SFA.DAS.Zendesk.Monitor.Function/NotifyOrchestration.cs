using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
[assembly: InternalsVisibleTo("SFA.DAS.Zendesk.Monitor.UnitTests")]
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

    public static partial class NotifyOrchestration
    {
        [Function("NotifyTicket")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData request,
            [DurableClient] DurableTaskClient starter,
            FunctionContext functionContext)
        {
            var input = await request.GetJsonBody<NotifyTicket, NotifyTicketValidator>();

            return await input.Match(
                valid => StartNotifyTicket(valid),
                invalid => Task.FromResult(request.BadRequest(invalid)));

            async Task<HttpResponseData> StartNotifyTicket(NotifyTicket ticket)
            {
                var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(nameof(WatcherOrchestration.ShareListedTickets), new[] { ticket.Id });
                return await starter.CreateCheckStatusResponseAsync(request, instanceId);
            }
        }
    }
}