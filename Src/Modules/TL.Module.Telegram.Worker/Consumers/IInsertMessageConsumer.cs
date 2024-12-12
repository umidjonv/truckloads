namespace TL.Module.Telegram.Worker.Consumers;

public interface IInsertMessageConsumer
{
    Task Consume(CancellationToken cancellationToken = default);
}