using MongoDB.Driver;
using Order.Api.Models;
using Order.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. MongoDb Connection
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://mongodb:27017";
    return new MongoClient(connStr);
});

builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("OrdersDb").GetCollection<OrderItem>("OrderItems");
});

// 2. Native RabbitMQ Publisher Service
builder.Services.AddSingleton<RabbitMqPublisher>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// 3. Create Order Endpoint
app.MapPost("/api/orders", async (
    CreateOrderDto dto,
    IMongoCollection<OrderItem> collection,
    RabbitMqPublisher publisher) =>
{
    var order = new OrderItem
    {
        ProductId = dto.ProductId,
        Quantity = dto.Quantity,
        TotalAmount = dto.TotalAmount
    };

    await collection.InsertOneAsync(order);

    // Publish message via native RabbitMQ.Client driver
    await publisher.PublishOrderCreatedAsync(new
    {
        OrderId = order.Id,
        order.ProductId,
        order.Quantity,
        order.TotalAmount,
        CreatedAt = DateTime.UtcNow
    });

    return Results.Created($"/api/orders/{order.Id}", order);
});

app.Run();

public record CreateOrderDto(string ProductId, int Quantity, decimal TotalAmount);