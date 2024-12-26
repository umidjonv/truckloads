using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TL.Api.Helpers;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Api.Endpoints;

public sealed class TelegramSettingEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("telegram-settings");

        group.MapPut("/insert", Insert);
        group.MapGet("/get", Get);
    }

    private static Task Insert(
        [FromServices] IMediator mediator,
        [FromBody] InsertSettingsParams request,
        CancellationToken cancellationToken = default)
    {
        return mediator.Send(request, cancellationToken);
    }

    private static Task<GetTelegramSettingsResult> Get(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        return mediator.Send(new GetTelegramSettingsParams(), cancellationToken);
    }
}