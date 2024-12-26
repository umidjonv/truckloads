using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using TdLib;

namespace TL.Module.Telegram.Extensions;

public static class TelegramExtension
{
    private static TdClient _client;
    private static bool _isConnected;
    private static string apiHash = string.Empty;
    private static int apiId = 0;
    
    private static Task SetParameters(this TdClient client, string apiHash, int apiId)
    {
        return client.ExecuteAsync(new TdApi.SetTdlibParameters()
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

    public static async Task<TdClient> GetClient([Required] string apiHash,[Required] int apiId)
    {
        if (_isConnected)
            return _client;
        
        _client = new TdClient();
        await _client.SetParameters(apiHash, apiId);

        _isConnected = true;
        return _client;
    }
}