using System.Threading;
using System.Threading.Tasks;
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
    public async Task Handle(InsertSettingsParams request, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Settings
            .AddAsync(mapper.Map<TelegramSettings>(request), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}