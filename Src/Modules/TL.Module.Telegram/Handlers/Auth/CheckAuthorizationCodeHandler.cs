using MediatR;
using Microsoft.Extensions.Logging;
using TdLib;
using TL.Module.Telegram.Extensions;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Auth;

public class CheckAuthorizationCodeHandler(
    ILogger<CheckAuthorizationCodeHandler> logger,
    IMediator mediator) : IRequestHandler<CheckTelegramAuthorizationCodeParams, CheckTelegramAuthorizationCodeResults>
{
    public async Task<CheckTelegramAuthorizationCodeResults> Handle(CheckTelegramAuthorizationCodeParams request,
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

        if (stateResult.State is not TdApi.AuthorizationState.AuthorizationStateWaitCode)
            throw new ArgumentException("Invalid authorization. Current state is {0}",
                stateResult.State.ToString());

        try
        {
            await SetAuthorizationCode(client, request.Code);

            return new CheckTelegramAuthorizationCodeResults(true);
        }
        catch (Exception e)
        {
            logger.LogError("Invalid authorization code. Details: {0}", e.Message);
            return new CheckTelegramAuthorizationCodeResults(false);
        }
    }

    private static Task SetAuthorizationCode(TdClient client, string code)
    {
        return client.ExecuteAsync(new TdApi.CheckAuthenticationCode
        {
            Code = code
        });
    }
}