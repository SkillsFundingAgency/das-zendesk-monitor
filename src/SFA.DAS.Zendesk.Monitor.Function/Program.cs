using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Zendesk.Monitor;
using MW = SFA.DAS.Zendesk.Monitor.Middleware;
using ZD = SFA.DAS.Zendesk.Monitor.Zendesk;
using System;
using ZenWatchFunction;

namespace ZenWatchFunction
{
    static class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults((context, builder) =>
                {
                    var config = context.Configuration;

                    builder.Services.AddLogging();
                    builder.Services.AddTransient<DurableWatcher>();
                    builder.Services.AddTransient<Watcher>();
                    builder.Services.AddTransient<ZD.SharingTickets>();
                    builder.Services.AddTransient<ZD.ISharingTickets>(
                        s => s.GetRequiredService<ZD.SharingTickets>());

                    builder.Services
                        .AddHttpClient<ZD.IApi>("ZendeskAPI")
                        .AddTypedClient(client =>
                            ZD.ApiFactory.CreateApi(client,
                                new Uri(config["Zendesk:Url"]),
                                config["Zendesk:ApiUser"],
                                config["Zendesk:ApiKey"]));

                    builder.Services
                        .AddHttpClient<MW.IApi>("MiddlewareAPI")
                        .AddTypedClient(client =>
                            MW.ApiFactory.CreateApi(client,
                                new Uri(config["Middleware:Url"]),
                                config["Middleware:SubscriptionKey"],
                                config["Middleware:ApiBasicAuth"]));
                })
                .Build();

            host.Run();
        }
    }
}