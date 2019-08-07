using RestEase;
using System.Threading.Tasks;

namespace ZenWatch.Middleware
{
    public interface IApi
    {
        [Post("/event")]
        Task PostEvent([Body] EventWrapper body);
    }
}