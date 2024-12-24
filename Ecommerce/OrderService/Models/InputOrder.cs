namespace OrderService.Models
{
    public class InputOrder
    {
        public int CustomerId { get; set; }
        public List<InputOrderItem> OrderItems { get; set; }
    }
}
