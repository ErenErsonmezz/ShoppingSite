namespace ShoppingSite.Models
{
    public class OrderDetailsViewModel
    {
        public IEnumerable<OrderDetails> OrderDetails { get; set; }
        public OrderHeader OrderHeader { get; set; }
    }
}
