using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TL.Module.Telegram.Domain;

namespace TL.Module.Telegram.Extensions;

public static class TelegramModuleDependencyInjection
{
    public static IServiceCollection AddTelegramModule(this IServiceCollection services,
        IConfigurationManager configurationManager)
    {
        services.AddMediatR(s => s.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddDbContextFactory<TelegramDbContext>(options =>
        {
            options.UseNpgsql(configurationManager.GetConnectionString("TelegramConnectionString"))
                .UseLazyLoadingProxies();
        });

        return services;
    }
}