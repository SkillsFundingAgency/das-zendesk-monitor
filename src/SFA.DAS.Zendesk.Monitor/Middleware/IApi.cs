using RestEase;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Middleware
{
    public interface IApi
    {
        [Delete("/ticket?subscription-key=1af517e259e34af59a8f0bbb12a9c28f")]
        Task PostEvent([Body] EventWrapper body);

        [Delete("/ticket?subscription-key=1af517e259e34af59a8f0bbb12a9c28f")]
        Task PostEvent([Body] EW2 body);
    }
}