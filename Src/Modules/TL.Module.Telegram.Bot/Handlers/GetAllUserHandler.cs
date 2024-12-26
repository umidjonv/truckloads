using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Bot.Handlers;

public class GetAllUserHandler(IDbContextFactory<TelegramDbContext> contextFactory)
    : IRequestHandler<GetAllUserParams, List<UserParams>>
{
    public async Task<List<UserParams>> Handle(GetAllUserParams request, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Users
            .Select(user => new UserParams()
            {
                ChatId = user.ChatId,
                UserId = user.UserId,
                Username = user.Username
            })
            .ToListAsync(cancellationToken);
    }
}