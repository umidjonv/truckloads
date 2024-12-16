namespace TL.Module.Telegram.Bot.Consumer;

public interface ITelegramBotUpdateConsumer
{
    Task StartReceiving(CancellationToken cancellationToken = default);
}