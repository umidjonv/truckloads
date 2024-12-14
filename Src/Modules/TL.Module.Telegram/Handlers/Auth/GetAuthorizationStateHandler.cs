using MediatR;
using Microsoft.Extensions.Logging;
using TdLib;
using TL.Module.Telegram.Extensions;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Auth;

public class GetAuthorizationStateHandler(
    ILogger<GetAuthorizationStateHandler> logger,
    IMediator mediator) : IRequestHandler<GetTelegramAuthorizationStateParams<TdApi.AuthorizationState>,
    GetTelegramAuthorizationStateResult<TdApi.AuthorizationState>>
{
    public async Task<GetTelegramAuthorizationStateResult<TdApi.AuthorizationState>> Handle(
        GetTelegramAuthorizationStateParams<TdApi.AuthorizationState> request,
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

        var state = await client.GetAuthorizationStateAsync();

        return new GetTelegramAuthorizationStateResult<TdApi.AuthorizationState>(state);
    }
}