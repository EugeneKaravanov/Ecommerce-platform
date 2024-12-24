using OrderService.Models;

namespace OrderService.Repositories
{
    public interface IOrderRepository
    {
        public Task<Result> CreateOrderAsync(InputOrder order, CancellationToken cancellationToken = default);
        public Task<List<OutputOrder>> GetOrdersAsync(CancellationToken cancellationToken = default);
        public Task<ResultWithValue<OutputOrder>> GetOrderAsync(int id, CancellationToken cancellationToken = default);
        public Task<ResultWithValue<List<OutputOrder>>> GetOrdersByCustomerAsync(int customerId, CancellationToken cancellationToken = default);
    }
}
