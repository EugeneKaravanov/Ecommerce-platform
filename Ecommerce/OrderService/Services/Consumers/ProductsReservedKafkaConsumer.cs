using Confluent.Kafka;
using MassTransit;
using Microsoft.AspNetCore.Components.Forms;
using OrderService.Models;
using OrderService.Models.Kafka.KafkaMessages;
using OrderService.Repositories;
using OrderService.Utilities;
using StackExchange.Redis;

namespace OrderService.Services.Consumers
{
    public class ProductsReservedKafkaConsumer : IConsumer<ProductsReserved>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ITopicProducer<OrderFormed> _producer;

        public ProductsReservedKafkaConsumer(IOrderRepository orderRepository, ITopicProducer<OrderFormed> producer)
        {
            _orderRepository = orderRepository;
            _producer = producer;
        }

        public Task Consume(ConsumeContext<ProductsReserved> context)
        {
            if (context.Message.Status == Status.Failure)
                return Task.CompletedTask;

            ResultWithValue<OrderFormed> result;

            return Task.Run(async () =>
            {
                result = await _orderRepository.CreateOrderAsync(context.Message);
                await _producer.Produce(result.Value);
            });
        }
    }
}
