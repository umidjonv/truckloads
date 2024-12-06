using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Module.Telegram.Domain.Entities;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Messages;

public class InsertMessageHandler(IDbContextFactory<TelegramDbContext> contextFactory)
    : IRequestHandler<InsertMessageParams>
{
    private readonly ITelegramDbContext _context = contextFactory.CreateDbContext();

    public async Task Handle(InsertMessageParams request, CancellationToken cancellationToken)
    {
        await _context.Messages.AddAsync(new TelegramMessage
        {
            ChatId = request.ChatId,
            Message = request.Message,
        }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}