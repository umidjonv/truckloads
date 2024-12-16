using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Bot.Handlers;

public class GetAllUserHandler(IDbContextFactory<TelegramDbContext> contextFactory)
    : IRequestHandler<GetAllUserParams, List<UserParams>>
{
    private readonly ITelegramDbContext _context = contextFactory.CreateDbContext();

    public async Task<List<UserParams>> Handle(GetAllUserParams request, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Select(user => new UserParams()
            {
                ChatId = user.ChatId,
                UserId = user.UserId,
                Username = user.Username
            })
            .ToListAsync(cancellationToken);
    }
}