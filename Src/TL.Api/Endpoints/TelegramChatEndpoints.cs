using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TL.Api.Helpers;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Api.Endpoints;

public class TelegramChatEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("chats");

        group.MapGet("/get", Get);
        group.MapPut("/switch-allow-chat", SwitchAllowChat);
    }

    private static Task<List<GetAllChatsResult>> Get(
        [FromServices]IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        return mediator.Send(new GetAllChatsParams(), cancellationToken);
    }

    private static Task SwitchAllowChat(
        [FromServices] IMediator mediator,
        SwitchAllowChatParams request,
        CancellationToken cancellationToken = default)
    {
        return mediator.Send(request, cancellationToken);
    }
}