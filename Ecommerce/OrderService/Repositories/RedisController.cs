using OrderService.Models;
using OrderService.Models.Redis;
using StackExchange.Redis;
using System.Text.Json;

namespace OrderService.Repositories
{
    public class RedisController
    {
        private readonly IDatabase _redisDb;
        private readonly int _ttl;

        public RedisController(RedisInfo redisInfo)
        {
            _redisDb = ConnectionMultiplexer.Connect(redisInfo.Address).GetDatabase();
            _ttl = redisInfo.Ttl;
        }

        public async Task<ResultWithValue<OutputOrder>> TryGetOrderFromCache(int id)
        {
            ResultWithValue<OutputOrder> result = new();
            result.Value = new();
            var jsonOrder = await _redisDb.StringGetAsync(id.ToString());

            if (jsonOrder.IsNullOrEmpty)
            {
                result.Status = Status.NotFound;

                return result;
            }

            result.Value = JsonSerializer.Deserialize<OutputOrder>(jsonOrder);
            result.Status = Status.Success;
            await _redisDb.KeyExpireAsync(id.ToString(), TimeSpan.FromSeconds(_ttl));

            return result;
        }

        public async Task AddOrderToCache(int id, OutputOrder order)
        {
            var jsonOrder = JsonSerializer.Serialize(order);

            await _redisDb.StringSetAsync(id.ToString(), jsonOrder);
            await _redisDb.KeyExpireAsync(id.ToString(), TimeSpan.FromSeconds(_ttl));
        }
    }
}
