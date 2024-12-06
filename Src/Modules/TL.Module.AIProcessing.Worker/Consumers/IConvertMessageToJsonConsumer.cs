namespace TL.Module.AIProcessing.Worker.Consumers;

public interface IConvertMessageToJsonConsumer
{
    Task Consume(CancellationToken cancellationToken = default);
}