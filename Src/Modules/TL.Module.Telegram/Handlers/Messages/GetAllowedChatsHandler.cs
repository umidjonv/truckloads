using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Messages;

public class GetAllowedChatsHandler(
    IDbContextFactory<TelegramDbContext> contextFactory)
    : IRequestHandler<GetAllowedChatIdsParams, GetAllowedChatsResult>
{

    public async Task<GetAllowedChatsResult> Handle(GetAllowedChatIdsParams request,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return new GetAllowedChatsResult(
            await context.Chats
                .AsNoTracking()
                .Where(s => s.IsAllowed)
                .Select(s => s.ChatId)
                .ToArrayAsync(cancellationToken));
    }
}