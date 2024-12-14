using MediatR;
using Microsoft.AspNetCore.Mvc;
using TL.Api.Helpers;

namespace TL.Api.Endpoints;

public sealed class TelegramSettingEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("telegram-settings");

        group.MapPost("/insert", Insert);
    }

    private static Task Insert(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}