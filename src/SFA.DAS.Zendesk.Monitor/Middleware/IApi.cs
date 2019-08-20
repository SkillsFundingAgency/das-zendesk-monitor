using RestEase;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor.Middleware
{
    public interface IApi
    {
        [Post("/event")]
        Task PostEvent([Body] EventWrapper body);

        [Post("/event")]
        Task PostEvent([Body] EW2 body);
    }
}