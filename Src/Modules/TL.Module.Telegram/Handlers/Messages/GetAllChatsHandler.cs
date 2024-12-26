using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Messages;

public class GetAllChatsHandler(
    IDbContextFactory<TelegramDbContext> contextFactory)
    : IRequestHandler<GetAllChatsParams, List<GetAllChatsResult>>
{
    private readonly ITelegramDbContext _context = contextFactory.CreateDbContext();

    public Task<List<GetAllChatsResult>> Handle(GetAllChatsParams request, CancellationToken cancellationToken)
    {
        return _context.Chats
            .AsNoTracking()
            .Select(s => new GetAllChatsResult(s.ChatId, s.ChatName, s.IsAllowed))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}