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
    private readonly ITelegramDbContext _context = contextFactory.CreateDbContext();

    public async Task<GetAllowedChatsResult> Handle(GetAllowedChatIdsParams request,
        CancellationToken cancellationToken)
    {
        return new GetAllowedChatsResult(
            await _context.Chats
                .AsNoTracking()
                .Where(s => s.IsAllowed)
                .Select(s => s.ChatId)
                .ToArrayAsync(cancellationToken));
    }
}