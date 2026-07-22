using Inventory.Api.Contracts;
using Inventory.Api.Models;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Inventory.Api.Services;

public class OrderConsumerWorker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderConsumerWorker> _logger;

    public OrderConsumerWorker(
        IConfiguration config,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderConsumerWorker> logger)
    {
        _config = config;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:Host"] ?? "rabbitmq",
            UserName = "guest",
            Password = "guest"
        };

        IConnection? connection = null;
        IChannel? channel = null; // Defined here so it's in scope for the rest of the method!

        // Retry loop to wait for RabbitMQ startup
        while (!stoppingToken.IsCancellationRequested && channel == null)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(stoppingToken);
                channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            }
            catch
            {
                _logger.LogWarning("Waiting for RabbitMQ container to accept connections...");
                await Task.Delay(3000, stoppingToken);
            }
        }

        if (channel == null) return;

        // Configure Exchange & Queue
        await channel.ExchangeDeclareAsync("order-exchange", ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync("inventory-order-created-queue", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync("inventory-order-created-queue", "order-exchange", routingKey: string.Empty, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Received Event in Inventory API: {Message}", json);

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var orderEvent = JsonSerializer.Deserialize<OrderCreatedMessage>(json, options);

                if (orderEvent == null || string.IsNullOrEmpty(orderEvent.ProductId))
                {
                    _logger.LogWarning("Invalid message structure received. Acknowledging and skipping.");
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                using (var scope = _scopeFactory.CreateScope())
                {
                    var inventoryCollection = scope.ServiceProvider
                        .GetRequiredService<IMongoCollection<InventoryItem>>();

                    // 1. Resolve the new OrderLogs collection
                    var orderLogsCollection = scope.ServiceProvider
                        .GetRequiredService<IMongoCollection<OrderLog>>();

                    // 2. Update stock level in Inventory collection
                    var filter = Builders<InventoryItem>.Filter.Eq(x => x.ProductId, orderEvent.ProductId);
                    var update = Builders<InventoryItem>.Update.Inc(x => x.Quantity, -orderEvent.Quantity);

                    await inventoryCollection.UpdateOneAsync(
                        filter,
                        update,
                        new UpdateOptions { IsUpsert = true },
                        cancellationToken: stoppingToken
                    );

                    // 3. Log the processed order into OrderLogs collection
                    var logEntry = new OrderLog
                    {
                        OrderId = orderEvent.OrderId,
                        ProductId = orderEvent.ProductId,
                        Quantity = orderEvent.Quantity,
                        TotalAmount = orderEvent.TotalAmount,
                        ProcessedAt = DateTime.UtcNow
                    };

                    await orderLogsCollection.InsertOneAsync(logEntry, cancellationToken: stoppingToken);

                    _logger.LogInformation(
                        "Deducted inventory & logged Order {OrderId} for Product {ProductId}",
                        orderEvent.OrderId,
                        orderEvent.ProductId
                    );
                }

                // Acknowledge message using the channel variable in scope
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OrderCreated message inside Inventory.Api");
            }
        };

        await channel.BasicConsumeAsync("inventory-order-created-queue", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}