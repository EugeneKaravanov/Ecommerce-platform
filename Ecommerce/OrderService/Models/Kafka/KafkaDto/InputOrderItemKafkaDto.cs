namespace OrderService.Models.Kafka.KafkaDto
{
    public class InputOrderItemKafkaDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
