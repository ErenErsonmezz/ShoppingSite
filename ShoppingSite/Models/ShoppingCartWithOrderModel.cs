namespace ShoppingSite.Models
{
    public class ShoppingCartWithOrderModel
    {
        public IEnumerable<ShoppingCart> Carts { get; set; }
        public OrderHeader OrderHeader { get; set; }
    }
}
