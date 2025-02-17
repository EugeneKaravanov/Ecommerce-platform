using InformerService.Models.Kafka.KafkaMessages;
using MassTransit;

namespace InformerService.Services.Consumers
{
    public class OrderFormedKafkaConsumer : IConsumer<OrderFormed>
    {
        private readonly UserInformer _userInformer;

        public OrderFormedKafkaConsumer(UserInformer userInformer)
        {
            _userInformer = userInformer;
        }

        public Task Consume(ConsumeContext<OrderFormed> context)
        {
            _userInformer.InformUserAboutOrder(context.Message);
            return Task.CompletedTask;
        }
    }
}
