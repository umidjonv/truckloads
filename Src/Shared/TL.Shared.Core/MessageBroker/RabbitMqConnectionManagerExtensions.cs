using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TL.Shared.Core.MessageBroker;

public static class RabbitMqConnectionManagerExtensions
{
    public static IServiceCollection AddRabbitMqService(this IServiceCollection services)
    {
        services.AddSingleton(async (provider) =>
        {
            var logger = provider.GetRequiredService<ILogger<RabbitMqConnectionManager>>();
            var configurationManager = provider.GetRequiredService<IConfigurationManager>();
            try
            {
                var rabbit = new RabbitMqConnectionManager(logger, configurationManager);
                await rabbit.Connect();
                return rabbit;
            }
            catch (Exception e)
            {
                logger.LogError("[{0}] Connection failed. Details: {1}", nameof(RabbitMqConnectionManager), e.Message);
                return new RabbitMqConnectionManager(logger, configurationManager);
            }
        });
        
        return services;
    }
}