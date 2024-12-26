using System;

namespace TL.Module.Telegram.Domain.Entities;

public class TelegramUser
{
    public Guid Id { get; set; }

    public long ChatId { get; set; }

    public long UserId { get; set; }

    public string Username { get; set; }
}