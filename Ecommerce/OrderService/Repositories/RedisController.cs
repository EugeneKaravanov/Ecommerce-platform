using OrderService.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace OrderService.Repositories
{
    public class RedisController
    {
        private readonly IDatabase _redisDb;

        public RedisController(string connectionString)
        {
            _redisDb = ConnectionMultiplexer.Connect(connectionString).GetDatabase();
        }

        public bool GetOrderFromCash(int id, out OutputOrder outputOrder)
        {
            var jsonOrder = _redisDb.StringGet(id.ToString());

            if (jsonOrder.IsNullOrEmpty)
            {
                outputOrder = null;
                return false;
            }

            outputOrder = JsonSerializer.Deserialize<OutputOrder>(_redisDb.StringGet(id.ToString()));

            return true;
        }

        public void AddOrderToCash(int id, OutputOrder order, int ttl)
        {
            var jsonOrder = JsonSerializer.Serialize(order);

            _redisDb.StringSet(id.ToString(), jsonOrder);
            _redisDb.KeyExpire(id.ToString(), TimeSpan.FromSeconds(ttl));
        }
    }
}
