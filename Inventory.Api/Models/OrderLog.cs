using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Inventory.Api.Models
{
    public class OrderLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string OrderId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
