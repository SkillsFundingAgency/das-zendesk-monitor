using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Zendesk.Monitor;
using ZD = SFA.DAS.Zendesk.Monitor.Zendesk;
using MW = SFA.DAS.Zendesk.Monitor.Middleware;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(ZenWatchFunction.Startup))]

namespace ZenWatchFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
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
                return new MW.ApiFactory(new Uri(config["Middleware:Url"]), config["Middleware:ApiBasicAuth"], logger);
            });
            builder.Services.AddTransient(s => s.GetRequiredService<MW.ApiFactory>().Create());
        }
    }
}