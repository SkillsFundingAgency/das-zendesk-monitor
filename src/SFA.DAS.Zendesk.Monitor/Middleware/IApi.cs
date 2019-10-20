using RestEase;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Middleware
{
    public interface IApi
    {
        [Query("subscription-key")]
        string SubscriptionKey { get; set; }

        [Delete("/ticket")]
        Task PostEvent([Body] EventWrapper body);
    }
}