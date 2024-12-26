using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain.Entities;

namespace TL.Module.Telegram.Domain;

public interface ITelegramDbContext
{
    DbSet<TelegramSettings> Settings { get; }

    DbSet<TelegramMessage> Messages { get; }

    DbSet<TelegramChat> Chats { get; set; }

    DbSet<TelegramUser> Users { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = new());

    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new());

    int SaveChanges();

    int SaveChanges(bool acceptAllChangesOnSuccess);
}