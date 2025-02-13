using Confluent.Kafka;
using MassTransit;
using Microsoft.AspNetCore.Components.Forms;
using OrderService.Models;
using OrderService.Models.Kafka.KafkaMessages;
using OrderService.Repositories;
using OrderService.Utilities;

namespace OrderService.Services.Consumers
{
    public class ProductsReservedKafkaConsumer : IConsumer<ProductsReserved>
    {
        private readonly IOrderRepository _orderRepository;

        public ProductsReservedKafkaConsumer(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public Task Consume(ConsumeContext<ProductsReserved> context)
        {
            if (context.Message.Status == Status.Failure)
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                Result result = await _orderRepository.CreateOrderAsync(context.Message);
            });
        }
    }
}
