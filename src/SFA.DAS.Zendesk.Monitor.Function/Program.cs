using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.Zendesk.Monitor.Function.Extensions;
using ZenWatchFunction;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, configBuilder) =>
    {
        configBuilder.AddConfiguration();
    })
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        services.AddLogging();

        services.AddAllServices(config);

    }).Build();

host.Run();

