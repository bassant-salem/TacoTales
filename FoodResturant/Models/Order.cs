namespace FoodResturant.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string UserId { get; set; } = string.Empty;

        
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public ApplicationUser? User { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public bool CanAdvance() =>
            Status == OrderStatus.Pending ||
            Status == OrderStatus.Preparing ||
            Status == OrderStatus.Ready;

       
        public bool CanCancel() => Status == OrderStatus.Pending;

  
        public void Advance()
        {
            Status = Status switch
            {
                OrderStatus.Pending => OrderStatus.Preparing,
                OrderStatus.Preparing => OrderStatus.Ready,
                OrderStatus.Ready => OrderStatus.Delivered,
                _ => throw new InvalidOperationException(
                    $"Cannot advance order {OrderId} from terminal status {Status}.")
            };
        }

       
        public OrderStatus? NextStatus() =>
            Status switch
            {
                OrderStatus.Pending => OrderStatus.Preparing,
                OrderStatus.Preparing => OrderStatus.Ready,
                OrderStatus.Ready => OrderStatus.Delivered,
                _ => null
            };

      
        public string StatusBadgeClass() =>
            Status switch
            {
                OrderStatus.Pending => "warning",
                OrderStatus.Preparing => "info",
                OrderStatus.Ready => "primary",
                OrderStatus.Delivered => "success",
                OrderStatus.Cancelled => "secondary",
                _ => "secondary"
            };

        public string StatusLabel() =>
            Status switch
            {
                OrderStatus.Pending => "⏳ Pending",
                OrderStatus.Preparing => "👨‍🍳 Preparing",
                OrderStatus.Ready => "✅ Ready",
                OrderStatus.Delivered => "🎉 Delivered",
                OrderStatus.Cancelled => "❌ Cancelled",
                _ => "Unknown"
            };
    }
}
