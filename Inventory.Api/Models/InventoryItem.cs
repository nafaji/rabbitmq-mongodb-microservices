using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Inventory.Api.Models
{
    public class InventoryItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string ProductId { get; set; } = string.Empty;

        // Make sure this property exists with both { get; set; }
        public int Quantity { get; set; }
    }
}
