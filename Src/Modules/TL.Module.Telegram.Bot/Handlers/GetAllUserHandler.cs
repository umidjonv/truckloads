using MediatR;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver.Linq;
using TL.Module.Telegram.Domain;
using TL.Module.Telegram.Domain.Entities;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Bot.Handlers;

public class GetAllUserHandler(IDbContextFactory<TelegramDbContext> contextFactory) : IRequestHandler<GetAllUserParams,List<UserParams>>
{
    private readonly ITelegramDbContext _context = contextFactory.CreateDbContext();


    public async Task<List<UserParams>> Handle(GetAllUserParams request, CancellationToken cancellationToken)
    {
        var users = await MongoQueryable.ToListAsync(
            _context.Users.Where(a => a.UserId != 0), cancellationToken);
        return users.Select(user => new UserParams()
        {
            ChatId = user.ChatId,
            UserId = user.UserId,
            Username = user.Username
        }).ToList();
    }
}