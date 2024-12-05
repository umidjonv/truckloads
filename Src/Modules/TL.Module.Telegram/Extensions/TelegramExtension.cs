using TdLib;

namespace TL.Module.Telegram.Extensions;

internal static class TelegramExtension
{
    internal static Task SetParameters(this TdClient client, string apiHash, int apiId) =>
        client.ExecuteAsync(new TdApi.SetTdlibParameters()
        {
            DatabaseDirectory = "tdlib",
            UseMessageDatabase = true,
            UseSecretChats = true,
            ApiId = apiId,
            ApiHash = apiHash,
            SystemLanguageCode = "en",
            DeviceModel = "Desktop",
            SystemVersion = "Windows 11",
            ApplicationVersion = "1.0"
        });
}