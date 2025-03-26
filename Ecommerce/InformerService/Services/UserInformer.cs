using InformerService.Models.Kafka.KafkaMessages;

namespace InformerService.Services
{
    public class UserInformer
    {
        private readonly ILogger<UserInformer> _logger;

        public UserInformer(ILogger<UserInformer> logger)
        {
            _logger = logger;
        }

        public void InformUserAboutOrder(OrderFormed order)
        {
            _logger.LogInformation($"Заказ {order.Id} от пользователя {order.CustomerId} на сумму {order.TotalAmount} успешно сформирован!");
        }
    }
}
