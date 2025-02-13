using MassTransit;
using ProductService.Models;
using ProductService.Models.Kafka.KafkaMessages;
using ProductService.Repositories;
using ProductService.Utilities;

namespace ProductService.Services.Consumers
{
    public class OrderCreatedKafkaConsumer : IConsumer<OrderCreated>
    {
        private readonly IProductRepository _productRepository;
        private readonly ITopicProducer<ProductsReserved> _producer;

        public OrderCreatedKafkaConsumer(IProductRepository productRepository, ITopicProducer<ProductsReserved> producer)
        {
            _productRepository = productRepository;
            _producer = producer;
        }

        public async Task Consume(ConsumeContext<OrderCreated> context)
        {
            ResultWithValue<List<OutputOrderProduct>> result =  await _productRepository.TakeProducts(context.Message);
            ProductsReserved reserved = Mapper.TransferTakeProductsResultAndOrderCreatedToProductsReserved(result, context.Message);

            await _producer.Produce(reserved);
        }
    }
}
