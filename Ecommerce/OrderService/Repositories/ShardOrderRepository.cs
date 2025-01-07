using Dapper;
using Npgsql;
using OrderService.Models;
using OrderService.Services;
using OrderService.Utilities.Factories;
using ProductServiceGRPC;

namespace OrderService.Repositories
{
    public class ShardOrderRepository : IOrderRepository
    {
        private readonly ShardConnectionFactory _shardConnectionFactory;
        private readonly ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient _productServiceClient;

        public ShardOrderRepository(ShardConnectionFactory shardConnectionFactory, ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient productServiceClient)
        {
            _shardConnectionFactory = shardConnectionFactory;
            _productServiceClient = productServiceClient;
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
