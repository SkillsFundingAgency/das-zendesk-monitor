using RestEase;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Middleware
{
    public interface IApi
    {
        [Query("subscription-key")]
        string SubscriptionKey { get; set; }

        [Put("/ticket?escalate=true")]
        Task EscalateTicket([Body] EventWrapper body);

        [Post("/ticket")]
        Task HandOffTicket([Body] EventWrapper body);

        [Delete("/ticket")]
        Task SolveTicket([Body] EventWrapper body);
    }
}