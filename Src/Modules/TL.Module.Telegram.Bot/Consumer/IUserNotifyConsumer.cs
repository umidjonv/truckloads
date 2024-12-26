namespace TL.Module.Telegram.Bot.Consumer;

public interface IUserNotifyConsumer
{
    Task Consume(CancellationToken cancellationToken);
}