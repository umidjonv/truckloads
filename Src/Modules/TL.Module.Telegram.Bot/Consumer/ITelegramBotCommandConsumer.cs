using System.Threading;
using System.Threading.Tasks;

namespace TL.Module.Telegram.Bot.Consumer;

public interface ITelegramBotCommandConsumer
{
    Task ExecuteAsync(CancellationToken stoppingToken);
}