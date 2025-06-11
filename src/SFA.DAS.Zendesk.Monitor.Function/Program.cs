using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SFA.DAS.Zendesk.Monitor.Function.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, configBuilder) =>
    {
        configBuilder.AddConfiguration();
    })
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        services.AddOpenTelemetryRegistration(context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]!);

        services.AddAllServices(config);

    })
    .ConfigureLogging((hostingContext, logging) =>
    {
        logging.SetMinimumLevel(LogLevel.Information);

        logging.AddFilter("Microsoft", LogLevel.Warning);
        logging.AddFilter("System", LogLevel.Warning);
        logging.AddFilter("SFA.DAS.Assessor.Functions", LogLevel.Information);
    }).Build();

host.Run();

