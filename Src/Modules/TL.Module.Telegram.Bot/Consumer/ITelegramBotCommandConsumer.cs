namespace TL.Module.Telegram.Bot.Consumer;

public interface ITelegramBotCommandConsumer
{
    Task ExecuteAsync(CancellationToken stoppingToken);
}