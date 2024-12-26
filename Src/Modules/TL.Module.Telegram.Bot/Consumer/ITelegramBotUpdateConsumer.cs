using System.Threading;
using System.Threading.Tasks;

namespace TL.Module.Telegram.Bot.Consumer;

public interface ITelegramBotUpdateConsumer
{
    Task StartReceiving(CancellationToken cancellationToken = default);
}