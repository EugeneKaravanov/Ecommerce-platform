namespace OrderService.Models.Kafka.KafkaMessages
{
    public class OrderFormed
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
