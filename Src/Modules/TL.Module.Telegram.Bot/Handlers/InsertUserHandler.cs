using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Module.Telegram.Domain.Entities;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Bot.Handlers;

public class InsertUserHandler(IDbContextFactory<TelegramDbContext> contextFactory)
    : IRequestHandler<InsertUserParams, bool>
{
    public async Task<bool> Handle(InsertUserParams request, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Users.AddAsync(new TelegramUser()
        {
            ChatId = request.ChatId,
            UserId = request.UserId,
            Username = request.UserName
        }, cancellationToken);

        var result = await context.SaveChangesAsync(cancellationToken);
        return result > 0;
    }
}