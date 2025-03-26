using ProductService.Models.Kafka.KafkaDto;

namespace ProductService.Models.Kafka.KafkaMessages
{
    public class OrderCreated
    {
        public int CustomerId { get; set; }
        public List<InputOrderItemKafkaDto> OrderProducts { get; set; }
    }
}
