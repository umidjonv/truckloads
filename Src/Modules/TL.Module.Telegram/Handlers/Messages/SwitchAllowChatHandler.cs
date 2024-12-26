using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Messages;

public class SwitchAllowChatHandler(IDbContextFactory<TelegramDbContext> contextFactory)
    : IRequestHandler<SwitchAllowChatParams>
{
    public async Task Handle(SwitchAllowChatParams request, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var chat = await context.Chats.FirstOrDefaultAsync(s => s.ChatId == request.ChatId, cancellationToken);
        ArgumentNullException.ThrowIfNull(chat, "Chat");

        chat.IsAllowed = request.Allow;
        await context.SaveChangesAsync(cancellationToken);
    }
}