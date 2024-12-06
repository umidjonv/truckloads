namespace TL.Shared.Common.Dtos.Telegram;

public record GetChatNewMessageResult<T>(List<T> Messages);