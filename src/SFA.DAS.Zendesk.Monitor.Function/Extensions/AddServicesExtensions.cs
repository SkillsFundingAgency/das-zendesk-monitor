using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using ZenWatchFunction;
using MW = SFA.DAS.Zendesk.Monitor.Middleware;
using ZD = SFA.DAS.Zendesk.Monitor.Zendesk;

namespace SFA.DAS.Zendesk.Monitor.Function.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class AddServicesExtensions
    {
        public static IServiceCollection AddAllServices(
        this IServiceCollection services,
        IConfiguration config)
        {
            services.AddTransient<DurableWatcher>();
            services.AddTransient<Watcher>();
            services.AddTransient<ZD.SharingTickets>();
            services.AddTransient<ZD.ISharingTickets>(
                s => s.GetRequiredService<ZD.SharingTickets>());

            services.AddHttpClientsServices(config);
            return services;
        }

        public static IServiceCollection AddHttpClientsServices(
            this IServiceCollection services,
            IConfiguration config)
        {
            var zendeskUrl = config["ZendeskUrl"] ?? throw new ArgumentNullException("ZendeskUrl", "Zendesk URL is not configured.");
            var zendeskApiUser = config["ApiUser"] ?? throw new ArgumentNullException("ApiUser", "Zendesk API user is not configured.");
            var zendeskApiKey = config["ApiKey"] ?? throw new ArgumentNullException("ApiKey", "Zendesk API key is not configured.");

            services
                .AddHttpClient<ZD.IApi>("ZendeskAPI")
                .AddTypedClient(client =>
                    ZD.ApiFactory.CreateApi(client, new Uri(zendeskUrl), zendeskApiUser, zendeskApiKey));

            var middlewareUrl = config["MiddlewareUrl"] ?? throw new ArgumentNullException("MiddlewareUrl", "Middleware URL is not configured.");
            var middlewareSubscriptionKey = config["MiddlewareSubscriptionKey"] ?? throw new ArgumentNullException("MiddlewareSubscriptionKey", "Middleware subscription key is not configured.");
            var middlewareApiBasicAuth = config["MiddlewareApiBasicAuth"] ?? throw new ArgumentNullException("MiddlewareApiBasicAuth", "Middleware API basic auth is not configured.");

            services
                .AddHttpClient<MW.IApi>("MiddlewareAPI")
                .AddTypedClient(client =>
                    MW.ApiFactory.CreateApi(client, new Uri(middlewareUrl), middlewareSubscriptionKey, middlewareApiBasicAuth));

            return services;
        }

    }
}