namespace TL.Shared.Common.Dtos.Telegram;

public record GetTelegramChatNewMessageResult<T>(List<T> Messages);