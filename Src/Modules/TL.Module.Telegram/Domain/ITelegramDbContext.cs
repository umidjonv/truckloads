using Microsoft.EntityFrameworkCore;
using TL.Module.Telegram.Domain.Entities;

namespace TL.Module.Telegram.Domain;

public interface ITelegramDbContext
{
    DbSet<TelegramSettings> Settings { get; }
}