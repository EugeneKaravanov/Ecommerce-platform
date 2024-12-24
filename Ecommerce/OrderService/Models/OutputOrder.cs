namespace OrderService.Models
{
    public class OutputOrder
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OutputOrderItem> OrderItems { get; set; }
    }
}
