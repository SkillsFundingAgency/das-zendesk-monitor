using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFA.DAS.Zendesk.Monitor;
using System;
using System.Linq;
using MW = SFA.DAS.Zendesk.Monitor.Middleware;
using ZD = SFA.DAS.Zendesk.Monitor.Zendesk;

[assembly: FunctionsStartup(typeof(ZenWatchFunction.Startup))]

namespace ZenWatchFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = builder.Services.BuildServiceProvider()
                .GetRequiredService<IConfiguration>();

            builder.Services.AddTransient<DurableWatcher>();
            builder.Services.AddTransient<Watcher>();
            builder.Services.AddTransient<ZD.SharingTickets>();
            builder.Services.AddTransient<ZD.ISharingTickets>(s => s.GetRequiredService<ZD.SharingTickets>());
            builder.Services.AddSingleton(s =>
            {
                var config = s.GetRequiredService<IConfiguration>();
                var logger = s.GetRequiredService<ILogger<LoggingHttpClientHandler>>();

                var all = config.GetChildren().Select(x => x.Key);

                return new ZD.ApiFactory(new Uri(config["Zendesk:Url"]), config["Zendesk:ApiUser"], config["Zendesk:ApiKey"], logger);
            });
            builder.Services.AddTransient(s => s.GetRequiredService<ZD.ApiFactory>().CreateApi());
            builder.Services.AddTransient(s =>
            {
                var config = s.GetRequiredService<IConfiguration>();
                var logger = s.GetRequiredService<ILogger<LoggingHttpClientHandler>>();
                return new MW.ApiFactory(new Uri(config["Middleware:Url"]), config["Middleware:SubscriptionKey"], config["Middleware:ApiBasicAuth"], logger);
            });
            builder.Services.AddTransient(s => s.GetRequiredService<MW.ApiFactory>().Create());
            builder.Services.AddLogging(config);
        }
    }
}