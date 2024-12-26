using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TL.Shared.Core.MessageBroker;

public static class RabbitMqConnectionManagerExtensions
{
    public static IServiceCollection AddRabbitMqConnectionManager(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitMqConnectionManager>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<RabbitMqConnectionManager>>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            try
            {
                IRabbitMqConnectionManager rabbit = new RabbitMqConnectionManager(logger, configuration);
                return rabbit;
            }
            catch (Exception e)
            {
                logger.LogError("[{0}] Connection failed. Details: {1}", nameof(RabbitMqConnectionManager), e.Message);
                return null;
            }
        });

        return services;
    }
}