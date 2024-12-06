using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TL.Module.AIProcessing.Worker.Consumers;
using TL.Module.AIProcessing.Worker.Jobs;

namespace TL.Module.AIProcessing.Worker.Extensions;

public static class AIProcessingModuleDependencyInjection
{
    public static IServiceCollection AddAIProcessingModule(this IServiceCollection services)
    {
        services.AddMediatR(s => s.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddSingleton<IConvertMessageToJsonConsumer, ConvertMessageToJsonConsumer>();
        services.AddSingleton<IPostNotifierJob, PostNotifierJob>();

        return services;
    }
}