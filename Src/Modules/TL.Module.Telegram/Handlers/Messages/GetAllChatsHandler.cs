using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Messages;

public class GetAllChatsHandler(
    IDbContextFactory<TelegramDbContext> contextFactory)
    : IRequestHandler<GetAllChatsParams, List<GetAllChatsResult>>
{
    public async Task<List<GetAllChatsResult>> Handle(GetAllChatsParams request, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Chats
            .AsNoTracking()
            .Select(s => new GetAllChatsResult(s.ChatId, s.ChatName, s.IsAllowed))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}