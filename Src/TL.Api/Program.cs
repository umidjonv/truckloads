using System.Reflection;
using System.Threading;
using Hangfire;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TL.Api.Helpers;
using TL.Module.AIProcessing.Worker.Consumers;
using TL.Module.AIProcessing.Worker.Extensions;
using TL.Module.AIProcessing.Worker.Jobs;
using TL.Module.Telegram.Bot.Consumer;
using TL.Module.Telegram.Bot.Extensions;
using TL.Module.Telegram.Extensions;
using TL.Module.Telegram.Worker.Extensions;
using TL.Module.Telegram.Worker.Jobs;
using TL.Shared.Core.MessageBroker;
using TL.Shared.Core.Mongo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

builder.Services.AddHttpClient();

builder.Services.AddMapster();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMongoConnectionManager();
builder.Services.AddRabbitMqConnectionManager();

builder.Services.AddTelegramWorkerModule();
builder.Services.AddAIProcessingModule();
builder.Services.AddTelegramModule(builder.Configuration);
builder.Services.AddTelegramBotModule();

builder.Services.AddHangfire(configuration =>
{
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseInMemoryStorage();
});

builder.Services.AddHangfireServer();

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

var app = builder.Build();

app.UseHangfireDashboard();

using var scope = app.Services.CreateScope();

var cancellationTokenSource = new CancellationTokenSource();

var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
backgroundJobClient.Enqueue<IConvertMessageToJsonConsumer>(s => s.Consume(cancellationTokenSource.Token));
// backgroundJobClient.Enqueue<ITelegramBotUpdateConsumer>(s => s.StartReceiving(cancellationTokenSource.Token));
// backgroundJobClient.Enqueue<ITelegramBotCommandConsumer>(s => s.ExecuteAsync(cancellationTokenSource.Token));
// backgroundJobClient.Enqueue<IUserNotifyConsumer>(s => s.Consume(cancellationTokenSource.Token));

var recurringJobClient = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
recurringJobClient.AddOrUpdate<IPostNotifierJob>(
    $"{nameof(IPostNotifierJob)}.{nameof(IPostNotifierJob.Invoke)}",
    s => s.Invoke(cancellationTokenSource.Token),
    "*/12 * * * *");
recurringJobClient.AddOrUpdate<IParseMessageJob>(
    $"{nameof(IParseMessageJob)}.{nameof(IParseMessageJob.Invoke)}",
    s => s.Invoke(cancellationTokenSource.Token),
    "*/12 * * * *");
recurringJobClient.AddOrUpdate<IParseChatsJob>(
    $"{nameof(IParseChatsJob)}.{nameof(IParseChatsJob.Invoke)}",
    s => s.Invoke(cancellationTokenSource.Token),
    "*/12 * * * *");

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHangfireDashboard(new DashboardOptions()
{
    Authorization = [new HangfireDashboardAuthorization()]
});

app.MapEndpoints();

app.Run();