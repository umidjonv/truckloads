using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain.Entities;

namespace TL.Module.Telegram.Domain;

public class TelegramDbContext : DbContext, ITelegramDbContext
{
    public DbSet<TelegramSettings> Settings { get; set; }
}