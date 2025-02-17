using OrderService.Models;
using OrderService.Models.Kafka.KafkaMessages;

namespace OrderService.Repositories
{
    public interface IOrderRepository
    {
        public Task<ResultWithValue<OrderFormed>> CreateOrderAsync(ProductsReserved productsReserved, CancellationToken cancellationToken = default);
        public Task<List<OutputOrder>> GetOrdersAsync(CancellationToken cancellationToken = default);
        public Task<ResultWithValue<OutputOrder>> GetOrderAsync(int id, CancellationToken cancellationToken = default);
        public Task<ResultWithValue<List<OutputOrder>>> GetOrdersByCustomerAsync(int customerId, CancellationToken cancellationToken = default);
    }
}
