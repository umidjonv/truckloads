namespace TL.Module.Telegram.Domain.Entities;

public class TelegramMessage
{
    public Guid Id { get; set; }

    public long ChatId { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedDate { get; } = DateTime.Now;
}