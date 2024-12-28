using Dapper;
using Npgsql;
using OrderService.Models;
using OrderService.Services;
using ProductServiceGRPC;

namespace OrderService.Repositories
{
    public class ShardOrderRepository : IOrderRepository
    {

        public ShardOrderRepository()
        {

        }

        public async Task<Result> CreateOrderAsync(InputOrder order, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<List<OutputOrder>> GetOrdersAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<ResultWithValue<OutputOrder>> GetOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<ResultWithValue<List<OutputOrder>>> GetOrdersByCustomerAsync(int customerId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
