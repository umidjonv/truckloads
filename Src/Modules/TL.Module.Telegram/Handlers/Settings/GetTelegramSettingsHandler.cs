using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Handlers.Settings;

public class GetTelegramSettingsHandler(IDbContextFactory<TelegramDbContext> contextFactory, IMapper mapper)
    : IRequestHandler<GetTelegramSettingsParams, GetTelegramSettingsResult?>
{

    public async Task<GetTelegramSettingsResult?> Handle(GetTelegramSettingsParams request,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        if (!await context.Settings.AnyAsync(cancellationToken: cancellationToken))
            return null;

        var settings = await context.Settings
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var setting = settings
            .OrderByDescending(s => s.CreatedDate)
            .FirstOrDefault();

        return setting is null ? null : mapper.Map<GetTelegramSettingsResult?>(setting);
    }
}