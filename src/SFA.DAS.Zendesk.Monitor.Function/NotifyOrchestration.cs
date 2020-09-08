using FluentValidation;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
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

    public partial class NotifyOrchestration
    {
        [FunctionName("NotifyTicket")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage request,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var input = await request.GetJsonBody<NotifyTicket, NotifyTicketValidator>();

            return await input.Match(
                valid => StartNotifyTicket(valid),
                invalid => Task.FromResult(request.BadRequest(invalid)));

            async Task<HttpResponseMessage> StartNotifyTicket(NotifyTicket ticket)
            {
                var instanceId = await starter.StartNewAsync(nameof(WatcherOrchestration.ShareListedTickets), new[] { ticket.Id });
                return starter.CreateCheckStatusResponse(request, instanceId);
            }
        }
    }
}