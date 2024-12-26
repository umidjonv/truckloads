using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TL.Api.Helpers;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Api.Endpoints;

public sealed class TelegramAuthEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("account");

        group.MapPost("/login", Login);
        group.MapPost("/check-auth-code", CheckAuthCode);
        group.MapPost("/check-two-step-auth-password", CheckTwoStepAuthPassword);
    }

    private static Task Login(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        return mediator.Send(new LoginTelegramClientParams(), cancellationToken);
    }

    private static Task<CheckTelegramAuthorizationCodeResults> CheckAuthCode(
        [FromServices] IMediator mediator,
        [FromBody] CheckTelegramAuthorizationCodeParams request,
        CancellationToken cancellationToken = default)
    {
        return mediator.Send(request, cancellationToken);
    }

    private static Task<SetTwoStepTelegramAuthenticationPasswordResult> CheckTwoStepAuthPassword(
        [FromServices] IMediator mediator,
        [FromBody] SetTwoStepTelegramAuthenticationPasswordParams request,
        CancellationToken cancellationToken = default)
    {
        return mediator.Send(request, cancellationToken);
    }
}