using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace TL.Shared.Core.MessageBroker;

public class RabbitMqConnectionManager(ILogger logger, IConfigurationManager configurationManager)
    : IRabbitMqConnectionManager
{
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _isConnected;

    public async Task<(IConnection, IChannel)> Connect()
    {
        if (_isConnected)
            return (_connection!, _channel!);

        var host = configurationManager["RabbitMq:Host"] ??
                   throw new ArgumentNullException($"RabbitMq:Hos");
        var username = configurationManager["RabbitMq:Username"] ??
                       throw new ArgumentNullException($"RabbitMq:Username");
        var password = configurationManager["RabbitMq:Password"] ??
                       throw new ArgumentNullException($"RabbitMq:Password");
        var virtualHost = configurationManager["RabbitMq:VirtualHost"] ??
                          throw new ArgumentNullException($"RabbitMq:VirtualHost");

        var factory = new ConnectionFactory()
        {
            HostName = host,
            Port = int.Parse(configurationManager["RabbitMq:Port"] ?? "5672"),
            UserName = username,
            Password = password,
            VirtualHost = virtualHost
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        _isConnected = true;

        return (_connection, _channel);
    }

    public async Task PublishAsync(string exchange, string route, string queue, string payload, CancellationToken cancellationToken = default)
    {
        if (_channel is null)
        {
            logger.LogError("[{0}] Not connected to RabbitMQ", nameof(RabbitMqConnectionManager));
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

    public async Task PublishAsync<T>(string exchange, string route, string queue, T payload, CancellationToken cancellationToken = default) where T : class
    {
        if (_channel is null)
        {
            logger.LogError("[{0}] Not connected to RabbitMQ", nameof(RabbitMqConnectionManager));
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

    private static async Task DeclareAsync(IChannel channel, string exchange, string route, string queue, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(exchange: exchange, type: ExchangeType.Direct, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(queue, true, false, false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queue, exchange, route, cancellationToken: cancellationToken);
    }
}