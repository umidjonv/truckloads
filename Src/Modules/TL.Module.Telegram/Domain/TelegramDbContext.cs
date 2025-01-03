using System;
using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain.Entities;

namespace TL.Module.Telegram.Domain;

public class TelegramDbContext(DbContextOptions<TelegramDbContext> options) : DbContext(options), ITelegramDbContext
{
    public DbSet<TelegramSettings> Settings { get; set; }

    public DbSet<TelegramMessage> Messages { get; set; }

    public DbSet<TelegramChat> Chats { get; set; }

    public DbSet<TelegramUser> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableDetailedErrors();
        
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        
        base.OnConfiguring(optionsBuilder);
    }
}