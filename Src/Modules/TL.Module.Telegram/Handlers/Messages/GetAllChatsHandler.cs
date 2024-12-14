using MediatR;
using Microsoft.Extensions.Logging;
using TdLib;
using TL.Module.Telegram.Extensions;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Messages;

public class GetAllChatsHandler(
    ILogger<GetAllChatsHandler> logger,
    IMediator mediator) : IRequestHandler<GetAllTelegramChatsParams<TdApi.Chat>, GetAllTelegramChatsResult<TdApi.Chat>>
{
    public async Task<GetAllTelegramChatsResult<TdApi.Chat>> Handle(GetAllTelegramChatsParams<TdApi.Chat> request,
        CancellationToken cancellationToken)
    {
        var settings = await mediator.Send(new GetTelegramSettingsParams(), cancellationToken);
        if (settings is null)
        {
            logger.LogError("Settings not found!");
            throw new ArgumentException("Settings not found!");
        }

        var client = new TdClient();
        await client.SetParameters(settings.ApiHash, settings.ApiId);

        var stateResult = await mediator.Send(new GetTelegramAuthorizationStateParams<TdApi.AuthorizationState>(),
            cancellationToken);

        if (stateResult.State is not TdApi.AuthorizationState.AuthorizationStateReady)
            throw new ArgumentException("Invalid authorization. Current state is {0}",
                stateResult.State.ToString());

        var chatsData = await client.ExecuteAsync(new TdApi.GetChats
        {
            Limit = 20
        });

        var chats = new List<TdApi.Chat>();
        for (var i = 0; i < chatsData.ChatIds.Length; i++)
            chats.Add(await client.ExecuteAsync(new TdApi.GetChat
            {
                ChatId = chatsData.ChatIds[i]
            }));

        return new GetAllTelegramChatsResult<TdApi.Chat>(chats);
    }
}