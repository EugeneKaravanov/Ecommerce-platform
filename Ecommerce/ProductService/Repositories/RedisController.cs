using StackExchange.Redis;
using System.Text.Json;
using ProductService.Models;
using Microsoft.IdentityModel.Tokens;
using System.Transactions;
using ProductService.Models.Redis;

namespace ProductService.Repositories
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
                        result.Value.Price = (decimal)field.Value;
                        break;
                    case "Stock":
                        result.Value.Stock = (int)field.Value;
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
            var transaction = _redisDb.CreateTransaction();

            transaction.AddCondition(Condition.KeyExists(id.ToString()));

            var transactionTask = transaction.HashSetAsync(id.ToString(), "Name", newProduct.Name);

            transactionTask = transaction.HashSetAsync(id.ToString(), "Description", newProduct.Description);
            transactionTask = transaction.HashSetAsync(id.ToString(), "Price", newProduct.Price.ToString());
            transactionTask = transaction.HashSetAsync(id.ToString(), "Stock", newProduct.Stock);
            await transaction.ExecuteAsync();

            await _redisDb.KeyExpireAsync(id.ToString(), TimeSpan.FromSeconds(_ttl));
        }

        public async Task TryDeleteProductInCache(int id)
        {
            await _redisDb.KeyDeleteAsync(id.ToString());
        }

        public async Task DecreaseStocks (List<OutputOrderProduct> products)
        {
            foreach (OutputOrderProduct product in products)
            {
                var transaction = _redisDb.CreateTransaction();
                int currentStock;

                transaction.AddCondition(Condition.KeyExists(product.ProductId.ToString()));
                HashEntry[] productFromCache = await _redisDb.HashGetAllAsync(product.ProductId.ToString());

                if (productFromCache.IsNullOrEmpty())
                    continue;

                currentStock = (int)productFromCache.FirstOrDefault(entry => entry.Name == "Stock").Value;

                var transactionTask = transaction.HashSetAsync(product.ProductId.ToString(), "Stock", currentStock - product.Quantity);

                await transaction.ExecuteAsync();
                await _redisDb.KeyExpireAsync(product.ProductId.ToString(), TimeSpan.FromSeconds(_ttl));
            }
        }
    }
}
