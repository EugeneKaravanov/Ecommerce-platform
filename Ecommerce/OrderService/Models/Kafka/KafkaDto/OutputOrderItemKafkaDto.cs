namespace OrderService.Models.Kafka.KafkaDto
{
    public class OutputOrderItemKafkaDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
