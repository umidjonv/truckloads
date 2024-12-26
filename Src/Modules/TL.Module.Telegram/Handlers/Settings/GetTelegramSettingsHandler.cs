using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Settings;

public class GetTelegramSettingsHandler(IDbContextFactory<TelegramDbContext> contextFactory, IMapper mapper)
    : IRequestHandler<GetTelegramSettingsParams, GetTelegramSettingsResult?>
{
    private readonly ITelegramDbContext _context = contextFactory.CreateDbContext();

    public async Task<GetTelegramSettingsResult?> Handle(GetTelegramSettingsParams request,
        CancellationToken cancellationToken)
    {
        var settings = await _context.Settings
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        return settings is null ? null : mapper.Map<GetTelegramSettingsResult>(settings);
    }
}