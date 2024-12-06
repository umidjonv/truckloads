using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain.Entities;

namespace TL.Module.Telegram.Domain;

public interface ITelegramDbContext
{
    DbSet<TelegramSettings> Settings { get; }
    DbSet<TelegramMessage> Messages { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
    
    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken());
    
    int SaveChanges();
    
    int SaveChanges(bool acceptAllChangesOnSuccess);
    
}