using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Messages;

public class SwitchAllowChatHandler(IDbContextFactory<TelegramDbContext> contextFactory)
    : IRequestHandler<SwitchAllowChatParams>
{
    private readonly ITelegramDbContext _context = contextFactory.CreateDbContext();

    public async Task Handle(SwitchAllowChatParams request, CancellationToken cancellationToken)
    {
        var chat = await _context.Chats.FirstOrDefaultAsync(s => s.ChatId == request.ChatId, cancellationToken);
        ArgumentNullException.ThrowIfNull(chat, "Chat");

        chat.IsAllowed = request.Allow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}