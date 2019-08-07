using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using ZenWatch;

[assembly: FunctionsStartup(typeof(ZenWatchFunction.Startup))]

namespace ZenWatchFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddTransient<WatcherOnTheWalls>();
            builder.Services.AddTransient<Watcher>();
            builder.Services.AddTransient(_ => ZenWatch.Zendesk.ApiFactory.Create("esfa"));
            builder.Services.AddTransient(_ => ZenWatch.Middleware.ApiFactory.Create("f731-452e-ac7c"));
        }
    }
}