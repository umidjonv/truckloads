using MediatR;
using Microsoft.Extensions.Logging;
using TdLib;
using TL.Module.Telegram.Extensions;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Auth;

public class SetTwoStepAuthenticationPasswordHandler(
    ILogger<SetTwoStepAuthenticationPasswordHandler> logger,
    IMediator mediator) : IRequestHandler<SetTwoStepTelegramAuthenticationPasswordParams, SetTwoStepTelegramAuthenticationPasswordResult>
{
    public async Task<SetTwoStepTelegramAuthenticationPasswordResult> Handle(SetTwoStepTelegramAuthenticationPasswordParams request, CancellationToken cancellationToken)
    {
        var settings = await mediator.Send(new GetTelegramSettingsParams(), cancellationToken);
        if (settings is null)
        {
            logger.LogError("Settings not found!");
            throw new ArgumentException(message: "Settings not found!");
        }
        
        var client = new TdClient();
        await client.SetParameters(settings.ApiHash, settings.ApiId);

        var stateResult = await mediator.Send(new GetTelegramAuthorizationStateParams<TdApi.AuthorizationState>(),
            cancellationToken);
        
        if (stateResult.State is not TdApi.AuthorizationState.AuthorizationStateWaitPassword)
            throw new ArgumentException("Invalid authorization. Current state is {0}",
                stateResult.State.ToString());
        try
        {
            await SetTwoStepAuthenticationPassword(client, request.Password);

            return new SetTwoStepTelegramAuthenticationPasswordResult(true);
        }
        catch (Exception e)
        {
            logger.LogError("Invalid authorization code. Details: {0}", e.Message);
            return new SetTwoStepTelegramAuthenticationPasswordResult(false);
        }
    }
    
    private static Task SetTwoStepAuthenticationPassword(TdClient client, string password) =>
        client.ExecuteAsync(new TdApi.CheckAuthenticationPassword()
        {
            Password = password
        });
}