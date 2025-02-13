using ProductService.Models.Kafka.KafkaDto;

namespace ProductService.Models.Kafka.KafkaMessages
{
    public class ProductsReserved
    {
        public int CustomerId { get; set; }
        public Status Status { get; set; }
        public string Message { get; set; }
        public List<OutputOrderItemKafkaDto> OrderProducts { get; set; }
    }
}
