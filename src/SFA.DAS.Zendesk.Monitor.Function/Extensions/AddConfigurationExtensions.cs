using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using SFA.DAS.Configuration.AzureTableStorage;

namespace SFA.DAS.Zendesk.Monitor.Function.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class ConfigurationExtensions
    {
        public static void AddConfiguration(this IConfigurationBuilder builder)
        {
           builder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true);
            var config = builder.Build();

            builder.AddAzureTableStorage(options =>
            {
                options.ConfigurationKeys = config["ConfigNames"]?.Split(",") ?? [];
                options.StorageConnectionString = config["ConfigurationStorageConnectionString"];
                options.EnvironmentName = config["EnvironmentName"];
                options.PreFixConfigurationKeys = false;
            });
        }
    }
}