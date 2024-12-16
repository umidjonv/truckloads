namespace TL.Module.Telegram.Worker.Jobs;

public interface IParseChatsJob
{
    Task Invoke(CancellationToken cancellationToken = default);
}