using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Order.Api.Services;

public class RabbitMqPublisher
{
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IConfiguration config)
    {
        _config = config;
    }

    private async Task InitChannelAsync()
    {
        if (_channel != null && _channel.IsOpen) return;

        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:Host"] ?? "rabbitmq",
            UserName = "guest",
            Password = "guest"
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        // Declare a durable exchange
        await _channel.ExchangeDeclareAsync(
            exchange: "order-exchange",
            type: ExchangeType.Fanout,
            durable: true
        );
    }

    public async Task PublishOrderCreatedAsync<T>(T message)
    {
        await InitChannelAsync();

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        // Publish to exchange
        await _channel!.BasicPublishAsync(
            exchange: "order-exchange",
            routingKey: string.Empty,
            body: body
        );
    }
}