using System.Threading;
using System.Threading.Tasks;

namespace TL.Module.Telegram.Worker.Jobs;

public interface IParseMessageJob
{
    Task Invoke(CancellationToken cancellationToken = default);
}