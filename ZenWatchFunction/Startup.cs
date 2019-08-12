using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using ZenWatch;
using ZD = ZenWatch.Zendesk;
using MW = ZenWatch.Middleware;
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

                return new ZD.ApiFactoryFactory(config["Zendesk::Instance"], config["Zendesk::ApiUser"], config["Zendesk::ApiKey"]);
            });
            builder.Services.AddTransient(s => s.GetRequiredService<ZD.ApiFactoryFactory>().CreateApi());
            builder.Services.AddTransient(s =>
            {
                var config = s.GetRequiredService<IConfiguration>();
                return MW.ApiFactory.Create(new Uri(config["Middleware::Url"]));
            });
        }
    }
}