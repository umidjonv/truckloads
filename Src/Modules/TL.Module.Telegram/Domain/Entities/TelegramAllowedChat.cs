using System.ComponentModel.DataAnnotations;

namespace TL.Module.Telegram.Domain.Entities;

public class TelegramAllowedChat
{
    public Guid Id { get; set; }

    [MaxLength(255)] public required string ChatName { get; set; }

    [MaxLength(255)] public required string ChatId { get; set; }

    public bool IsActive { get; set; }

    public bool IsAllowed { get; set; }
}