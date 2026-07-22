namespace Inventory.Api.Contracts
{
    public record OrderCreatedMessage
    {
        public string OrderId { get; init; } = default!;
        public string ProductId { get; init; } = default!;
        public int Quantity { get; init; }
        public decimal TotalAmount { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
