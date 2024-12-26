using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using TdLib;
using TL.Module.Telegram.Extensions;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Messages;

public class GetTelegramChatNewMessagesHandler(
    ILogger<GetTelegramChatNewMessagesHandler> logger,
    IMediator mediator)
    : IRequestHandler<GetChatNewMessageParams<TdApi.Message>, GetTelegramChatNewMessageResult<TdApi.Message>>
{
    public async Task<GetTelegramChatNewMessageResult<TdApi.Message>> Handle(GetChatNewMessageParams<TdApi.Message> request,
        CancellationToken cancellationToken)
    {
        var settings = await mediator.Send(new GetTelegramSettingsParams(), cancellationToken);
        if (settings is null)
        {
            logger.LogError("Settings not found!");
            throw new ArgumentException("Settings not found!");
        }

        var client = await TelegramExtension.GetClient(settings.ApiHash, settings.ApiId);
        ;

        var stateResult = await mediator.Send(new GetTelegramAuthorizationStateParams<TdApi.AuthorizationState>(),
            cancellationToken);

        if (stateResult.State is not TdApi.AuthorizationState.AuthorizationStateWaitTdlibParameters.AuthorizationStateReady)
            throw new ArgumentException("Invalid authorization. Current state is {0}",
                stateResult.State.ToString());

        var messages = new List<TdApi.Message>();
        var lastMessageId = 0L;
        while (!cancellationToken.IsCancellationRequested)
        {
            var history = await client.ExecuteAsync(new TdApi.GetChatHistory()
            {
                ChatId = request.Id,
                Limit = 100,
                FromMessageId = lastMessageId,
                OnlyLocal = false
            });

            if (history.Messages_.Length == 0)
                break;

            messages.AddRange(history.Messages_);
            lastMessageId = history.Messages_[^1].Id;
        }

        return new GetTelegramChatNewMessageResult<TdApi.Message>(messages);
    }
}