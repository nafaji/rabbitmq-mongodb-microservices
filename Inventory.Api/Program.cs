using Inventory.Api.Models;
using Inventory.Api.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// 1. Register MongoDB Client
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("MongoDb")
        ?? "mongodb://mongodb:27017";

    var settings = MongoClientSettings.FromConnectionString(connectionString);
    settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

    return new MongoClient(settings);
});

// 2. Register Inventory Collection
builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("InventoryDb").GetCollection<InventoryItem>("InventoryItems");
});

// 3. Register OrderLogs Collection (NEW)
builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("InventoryDb").GetCollection<OrderLog>("OrderLogs");
});

// 4. Register Background Worker
builder.Services.AddHostedService<OrderConsumerWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();