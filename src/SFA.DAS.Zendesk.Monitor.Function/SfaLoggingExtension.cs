using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using SFA.DAS.NLog.Targets.Redis.DotNetCore;

namespace ZenWatchFunction
{
    public static class SfaLoggingExtension
    {
        public static IServiceCollection AddLogging(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddLogging(logging =>
            {
                LogManager.Configuration = LoggingConfiguration(configuration);

                logging.AddNLog(new NLogProviderOptions
                {
                    CaptureMessageTemplates = true,
                    CaptureMessageProperties = true,
                });
            });

            return services;
        }

        private static LoggingConfiguration LoggingConfiguration(IConfiguration configuration)
        {
            var appName = configuration?.GetValue<string>("AppName") ?? "das-zendesk-monitor";
            var loggingConf = new LoggingConfiguration();
            loggingConf.AddRedis(appName);
            return loggingConf;
        }

        public static LoggingConfiguration AddRedis(
            this LoggingConfiguration config, string appName)
        {
            var target = new RedisTarget
            {
                Name = "RedisLog",
                AppName = appName,
                EnvironmentKeyName = "EnvironmentName",
                ConnectionStringName = "LoggingRedisConnectionString",
                IncludeAllProperties = true,
                Layout = "${message}"
            };

            config.AddTarget(target);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, "RedisLog");

            return config;
        }
    }
}