﻿using Microsoft.Extensions.DependencyInjection;
using TL.Module.Telegram.Worker.Consumers;
using TL.Module.Telegram.Worker.Jobs;

namespace TL.Module.Telegram.Worker.Extensions;

public static class TelegramWorkerModuleDependencyInjection
{
    public static IServiceCollection AddTelegramWorkerModule(this IServiceCollection services)
    {
        services.AddSingleton<IParseMessageJob, ParseMessageJob>();
        services.AddSingleton<IInsertMessageConsumer, InsertMessageConsumer>();

        return services;
    }
}