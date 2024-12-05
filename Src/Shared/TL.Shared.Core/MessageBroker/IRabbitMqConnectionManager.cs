using RabbitMQ.Client;

namespace TL.Shared.Core.MessageBroker;

public interface IRabbitMqConnectionManager
{
    Task<(IConnection, IChannel)> Connect();

    Task PublishAsync(string exchange, string route, string queue, string payload,
        CancellationToken cancellationToken = default);

    Task PublishAsync<T>(string exchange, string route, string queue, T payload,
        CancellationToken cancellationToken = default) where T : class;
}