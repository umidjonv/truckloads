namespace TL.Shared.Common.Dtos.Telegram;

public record GetTelegramSettingsResult(
    string PhoneNumber,
    int ApiId,
    string ApiHash);