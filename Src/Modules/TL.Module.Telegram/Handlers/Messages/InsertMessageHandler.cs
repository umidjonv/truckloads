using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Module.Telegram.Domain.Entities;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Messages;

public class InsertMessageHandler(IDbContextFactory<TelegramDbContext> contextFactory)
    : IRequestHandler<InsertMessageParams>
{
    public async Task Handle(InsertMessageParams request, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        await context.Messages.AddAsync(new TelegramMessage
        {
            ChatId = request.ChatId,
            Message = request.Message
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}