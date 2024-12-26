using System;
using System.ComponentModel.DataAnnotations;

namespace TL.Module.Telegram.Domain.Entities;

public class TelegramSettings
{
    public Guid Id { get; set; }

    [MaxLength(20)] public required string PhoneNumber { get; set; } = string.Empty;

    public int ApiId { get; set; }

    [MaxLength(255)] public required string ApiHash { get; set; } = string.Empty;

    public DateTime CreatedDate { get; } = DateTime.Now;
}