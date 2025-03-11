using StackExchange.Redis;
using System.Text.Json;
using ProductService.Models;
using Microsoft.IdentityModel.Tokens;
using System.Transactions;
using ProductService.Models.Redis;
using ProductService.Utilities.Redis;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace ProductService.Repositories
{
    public class RedisController : IDisposable
    {
        private readonly IDatabase _redisDb;
        private readonly int _ttl;
        private readonly ConnectionMultiplexer _redis;

        public RedisController(RedisInfo redisInfo)
        {
            _redis = ConnectionMultiplexer.Connect(redisInfo.Address);
            _redisDb = _redis.GetDatabase();
            _ttl = redisInfo.Ttl;
        }

        public async Task<ResultWithValue<ProductWithId>> TryGetProductFromCache(int id)
        {
            ResultWithValue<ProductWithId> result = new();
            result.Value = new();
            HashEntry[] productFromCache = await _redisDb.HashGetAllAsync(id.ToString());

            if (productFromCache.IsNullOrEmpty())
            {
                result.Status = Status.NotFound;

                return result;
            }

            result.Value.Id = id;

            foreach (var field in productFromCache)
            {
                switch (field.Name)
                {
                    case "Name":
                        result.Value.Name = field.Value;
                        break;
                    case "Description":
                        result.Value.Description = field.Value;
                        break;
                    case "Price":
                        result.Value.Price = decimal.Parse(field.Value);
                        break;
                    case "Stock":
                        result.Value.Stock = int.Parse(field.Value);
                        break;
                }
            }

            await _redisDb.KeyExpireAsync(id.ToString(), TimeSpan.FromSeconds(_ttl));
            result.Status = Status.Success;

            return result;
        }

        public async Task AddProductToCache(int id, Product product)
        {
            HashEntry[] addingProduct = new HashEntry[]
            {
                new HashEntry("Name", product.Name),
                new HashEntry("Description", product.Description),
                new HashEntry("Price", product.Price.ToString()),
                new HashEntry("Stock", product.Stock)
            };

            await _redisDb.HashSetAsync(id.ToString(), addingProduct);
            await _redisDb.KeyExpireAsync(id.ToString(), TimeSpan.FromSeconds(_ttl));
        }

        public async Task TryUpdateProductInCache(int id, Product newProduct)
        {
            bool isTransactionSuccessful = await _redisDb.TransactAsync(commands => commands
                .Enqueue(transaction => transaction.HashSetAsync(id.ToString(), "Name", newProduct.Name))
                .Enqueue(transaction => transaction.HashSetAsync(id.ToString(), "Description", newProduct.Description))
                .Enqueue(transaction => transaction.HashSetAsync(id.ToString(), "Price", newProduct.Price.ToString()))
                .Enqueue(transaction => transaction.HashSetAsync(id.ToString(), "Stock", newProduct.Stock)));

            await _redisDb.KeyExpireAsync(id.ToString(), TimeSpan.FromSeconds(_ttl));

            if (isTransactionSuccessful == false)
            {
                Console.WriteLine("Транзакция не замкомитилась!");
            }
        }

        public async Task TryDeleteProductInCache(int id)
        {
            await _redisDb.KeyDeleteAsync(id.ToString());
        }

        public async Task DecreaseStocks (List<OutputOrderProduct> products)
        {
            var script = @"
                if redis.call('EXISTS', KEYS[1]) == 1 then
                    return redis.call('HINCRBY', KEYS[1], ARGV[1], -ARGV[2])
                else
                    return nil
                end
            ";

            foreach (OutputOrderProduct product in products)
            {
                await _redisDb.ScriptEvaluateAsync(script, new RedisKey[] { product.ProductId.ToString() }, new RedisValue[] { "Stock", product.Quantity });
                await _redisDb.KeyExpireAsync(product.ProductId.ToString(), TimeSpan.FromSeconds(_ttl));
            }
        }

        public void Dispose()
        {
            _redis.Dispose();
        }
    }
}
