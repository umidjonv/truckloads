using System.Threading;
using System.Threading.Tasks;

namespace TL.Module.AIProcessing.Worker.Consumers;

public interface IConvertMessageToJsonConsumer
{
    Task Consume(CancellationToken cancellationToken = default);
}