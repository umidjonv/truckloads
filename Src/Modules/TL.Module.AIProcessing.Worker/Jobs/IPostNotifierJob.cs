using System.Threading;
using System.Threading.Tasks;

namespace TL.Module.AIProcessing.Worker.Jobs;

public interface IPostNotifierJob
{
    Task Invoke(CancellationToken cancellationToken = default);
}