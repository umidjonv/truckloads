using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Module.Telegram.Domain.Entities;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Settings;

public class InsertTelegramSettingsHandler(IDbContextFactory<TelegramDbContext> contextFactory, IMapper mapper)
    : IRequestHandler<InsertSettingsParams>
{
    private readonly ITelegramDbContext _context = contextFactory.CreateDbContext();
    public async Task Handle(InsertSettingsParams request, CancellationToken cancellationToken)
    {
        await _context.Settings
            .AddAsync(mapper.Map<TelegramSettings>(request), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}