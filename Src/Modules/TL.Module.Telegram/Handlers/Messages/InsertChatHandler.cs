using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.Module.Telegram.Domain;
using TL.Module.Telegram.Domain.Entities;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Messages;

public class InsertChatHandler(
    IDbContextFactory<TelegramDbContext> contextFactory,
    IServiceScopeFactory serviceScopeFactory) : IRequestHandler<InsertChatParams>
{

    public async Task Handle(InsertChatParams request, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        if (!await context.Chats.AnyAsync(
                s => s.ChatId == request.ChatId,
                cancellationToken))
        {
            await context.Chats.AddAsync(mapper.Map<TelegramChat>(request), cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}