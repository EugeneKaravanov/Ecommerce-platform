using OrderService.Models.Kafka.KafkaDto;

namespace OrderService.Models.Kafka.KafkaMessages
{
    public record OrderCreated
    {
        public int CustomerId { get; set; }
        public List<InputOrderItemKafkaDto> OrderProducts { get; set; }
    }
}
