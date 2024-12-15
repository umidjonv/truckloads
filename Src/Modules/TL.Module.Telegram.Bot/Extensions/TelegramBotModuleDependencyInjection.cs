using Microsoft.Extensions.DependencyInjection;
using TL.Module.Telegram.Bot.Consumer;

namespace TL.Module.Telegram.Bot.Extensions;

public static class TelegramBotModuleDependencyInjection
{
    public static IServiceCollection AddTelegramBotModule(this IServiceCollection services)
    {
        services.AddSingleton<ITelegramBotCommandConsumer, TelegramBotCommandConsumer>();
        services.AddSingleton<ITelegramBotUpdateConsumer, TelegramBotUpdateConsumer>();
        
        return services;
    }
}