using System.ComponentModel.DataAnnotations;

namespace TL.Module.Telegram.Domain.Entities;

public class TelegramChat
{
    public Guid Id { get; set; }

    [MaxLength(255)] public required string ChatName { get; set; }

    public long ChatId { get; set; }

    public bool IsActive { get; set; }

    public bool IsAllowed { get; set; }
}