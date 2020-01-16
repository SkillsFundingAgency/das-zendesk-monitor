﻿using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Zendesk.Monitor;
using System;
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

            builder.Services.AddLogging(config);
            builder.Services.AddTransient<DurableWatcher>();
            builder.Services.AddTransient<Watcher>();
            builder.Services.AddTransient<ZD.SharingTickets>();
            builder.Services.AddTransient<ZD.ISharingTickets>(
                s => s.GetRequiredService<ZD.SharingTickets>());

            builder.Services
                .AddHttpClient<ZD.IApi>()
                .AddTypedClient(client =>
                    ZD.ApiFactory.CreateApi(client,
                        new Uri(config["Zendesk:Url"]),
                        config["Zendesk:ApiUser"],
                        config["Zendesk:ApiKey"]));

            builder.Services
                .AddHttpClient<MW.IApi>()
                .AddTypedClient(client =>
                    MW.ApiFactory.CreateApi(client,
                        new Uri(config["Middleware:Url"]),
                        config["Middleware:SubscriptionKey"],
                        config["Middleware:ApiBasicAuth"]));
        }
    }
}