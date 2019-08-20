using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Zendesk.Monitor;
using ZD = SFA.DAS.Zendesk.Monitor.Zendesk;
using MW = SFA.DAS.Zendesk.Monitor.Middleware;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System;

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

                var all = config.GetChildren().Select(x => x.Key);

                return new ZD.ApiFactoryFactory(new Uri(config["Zendesk:Url"]), config["Zendesk:ApiUser"], config["Zendesk:ApiKey"]);
            });
            builder.Services.AddTransient(s => s.GetRequiredService<ZD.ApiFactoryFactory>().CreateApi());
            builder.Services.AddTransient(s =>
            {
                var config = s.GetRequiredService<IConfiguration>();
                return MW.ApiFactory.Create(new Uri(config["Middleware:Url"]));
            });
        }
    }
}