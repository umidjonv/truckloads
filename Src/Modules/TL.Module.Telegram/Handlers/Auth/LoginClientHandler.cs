using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using TdLib;
using TL.Module.Telegram.Extensions;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Auth;

public class LoginClientHandler(ILogger<LoginClientHandler> logger, IMediator mediator)
    : IRequestHandler<LoginTelegramClientParams>
{
    public async Task Handle(LoginTelegramClientParams request,
        CancellationToken cancellationToken)
    {
        var settings = await mediator.Send(new GetTelegramSettingsParams(), cancellationToken);
        if (settings is null)
        {
            logger.LogError("Settings not found!");
            throw new ArgumentException("Settings not found!");
        }

        var client = await TelegramExtension.GetClient(settings.ApiHash, settings.ApiId);
        
        var stateResult = await mediator.Send(new GetTelegramAuthorizationStateParams<TdApi.AuthorizationState>(),
            cancellationToken);
        
        if (stateResult.State is not TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber)
            throw new ArgumentException("Invalid authorization. Current state is {0}",
                stateResult.State.ToString());

        try
        {
            await SetPhoneNumber(client, settings.PhoneNumber);
        }
        catch (Exception e)
        {
            logger.LogError("Failed to set phone number. Details: {0}", e.Message);
            throw;
        }
    }

    private static Task SetPhoneNumber(TdClient client, string phoneNumber)
    {
        return client.ExecuteAsync(new TdApi.SetAuthenticationPhoneNumber
        {
            PhoneNumber = phoneNumber
        });
    }
}