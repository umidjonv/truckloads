using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace TL.Shared.Core.MessageBroker;

public class RabbitMqConnectionManager : IRabbitMqConnectionManager
{
    private readonly IConnection? _connection;
    private readonly IChannel? _channel;
    private readonly bool _isConnected;
    private readonly ILogger<RabbitMqConnectionManager> _logger;

    public RabbitMqConnectionManager(ILogger<RabbitMqConnectionManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        if (_isConnected)
            return;

        var host = configuration["RabbitMq:Host"] ??
                   throw new ArgumentNullException($"RabbitMq:Hos");
        var username = configuration["RabbitMq:Username"] ??
                       throw new ArgumentNullException($"RabbitMq:Username");
        var password = configuration["RabbitMq:Password"] ??
                       throw new ArgumentNullException($"RabbitMq:Password");
        var virtualHost = configuration["RabbitMq:VirtualHost"] ??
                          throw new ArgumentNullException($"RabbitMq:VirtualHost");

        var factory = new ConnectionFactory()
        {
            HostName = host,
            Port = int.Parse(configuration["RabbitMq:Port"] ?? "5672"),
            UserName = username,
            Password = password,
            VirtualHost = virtualHost
        };

        _connection = factory.CreateConnectionAsync()
            .GetAwaiter()
            .GetResult();
        _channel = _connection.CreateChannelAsync()
            .GetAwaiter()
            .GetResult();

        _isConnected = true;
    }

    public async Task<(IConnection, IChannel)> GetConnection()
    {
        return (_connection, _channel);
    }

    public async Task PublishAsync(string exchange, string route, string queue, string payload,
        CancellationToken cancellationToken = default)
    {
        if (_channel is null)
        {
            _logger.LogError("[{0}] Not connected to RabbitMQ", nameof(RabbitMqConnectionManager));
            return;
        }

        await DeclareAsync(_channel, exchange, route, queue, cancellationToken);

        var body = Encoding.UTF8.GetBytes(payload);

        await _channel.BasicPublishAsync(
            exchange,
            route,
            false,
            new BasicProperties()
            {
                Persistent = true
            },
            body,
            cancellationToken);
    }

    public async Task PublishAsync<T>(string exchange, string route, string queue, T payload,
        CancellationToken cancellationToken = default) where T : class
    {
        if (_channel is null)
        {
            _logger.LogError("[{0}] Not connected to RabbitMQ", nameof(RabbitMqConnectionManager));
            return;
        }

        await DeclareAsync(_channel, exchange, route, queue, cancellationToken);

        var message = JsonSerializer.Serialize(payload);
        var body = Encoding.UTF8.GetBytes(message);

        await _channel.BasicPublishAsync(
            exchange,
            route,
            false,
            new BasicProperties()
            {
                Persistent = true
            },
            body, cancellationToken);
    }

    private static async Task DeclareAsync(IChannel channel, string exchange, string route, string queue,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Direct, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(queue, true, false, false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queue, exchange, route, cancellationToken: cancellationToken);
    }
}