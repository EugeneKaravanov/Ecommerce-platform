namespace ProductService.Models.Kafka
{
    public class KafkaInfo
    {
        public string OrderCreatedTopic { get; set; }
        public string ProductsReservedTopic { get; set; }
        public string Address { get; set; }
    }
}
