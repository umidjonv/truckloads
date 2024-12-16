using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TdLib;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Worker.Jobs;

public class ParseChatsJob(IServiceScopeFactory serviceScopeFactory) : IParseChatsJob
{
    public async Task Invoke(CancellationToken cancellationToken = default)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ParseChatsJob>>();

        var state = await mediator.Send(new GetTelegramAuthorizationStateParams<TdApi.AuthorizationState>(),
            cancellationToken);

        if (state.State is not TdApi.AuthorizationState.AuthorizationStateReady)
        {
            logger.LogError("[{0}] Telegram is not in authorization state", nameof(ParseMessageJob));
            return;
        }

        var chats = await mediator.Send(new GetAllTelegramChatsParams<TdApi.Chat>(), cancellationToken);

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        await Parallel.ForEachAsync(chats.Chats, parallelOptions, async (chat, token) =>
        {
            try
            {
                await mediator.Send(new InsertChatParams(chat.Id, chat.Title), token);
            }
            catch (Exception e)
            {
                logger.LogError("[{0}] Error occurred while parsing chat {1}. Details: {2}",
                    nameof(ParseChatsJob),
                    chat.Id,
                    e.Message);
            }
        });
    }
}