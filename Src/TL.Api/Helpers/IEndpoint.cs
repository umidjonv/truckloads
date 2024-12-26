using Microsoft.AspNetCore.Routing;

namespace TL.Api.Helpers;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}