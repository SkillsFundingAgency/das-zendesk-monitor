using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.Zendesk.Monitor
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
    public class LoggingHttpClientHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHttpClientHandler> logger;

        public LoggingHttpClientHandler(ILogger<LoggingHttpClientHandler> logger)
            : base(new HttpClientHandler())
        {
            this.logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                using (logger.BeginScope("{url}", request.RequestUri))
                {
                    using (logger.BeginScope("{request}", request))
                        logger.LogTrace("Request: {url}", request.RequestUri);

                    var response = await base.SendAsync(request, cancellationToken);

                    using (logger.BeginScope("{response}", response))
                    using (logger.BeginScope("{response_body}", (await response.Content.ReadAsStringAsync()).Trim()))
                        logger.LogTrace("Response: {response}", response.StatusCode);

                    return response;
                }
            }
            else
            {
                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}