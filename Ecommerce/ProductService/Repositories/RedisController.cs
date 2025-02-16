using StackExchange.Redis;
using System.Text.Json;
using ProductService.Models;
using Microsoft.IdentityModel.Tokens;
using System.Transactions;

namespace ProductService.Repositories
{
    public class RedisController
    {
        private readonly IDatabase _redisDb;

        public RedisController(string connectionString)
        {
            _redisDb = ConnectionMultiplexer.Connect(connectionString).GetDatabase();
        }

        public bool TryGetProductFromCash(int id, int ttl, out ProductWithId product)
        {
            HashEntry[] productFromCash = _redisDb.HashGetAll(id.ToString());
            product = new();

            if (productFromCash.IsNullOrEmpty())
                return false;

            product.Id = id;

            foreach (var field in productFromCash)
            {
                switch (field.Name)
                {
                    case "Name":
                        product.Name = field.Value;
                        break;
                    case "Description":
                        product.Description = field.Value;
                        break;
                    case "Price":
                        product.Price = (decimal)field.Value;
                        break;
                    case "Stock":
                        product.Stock = (int)field.Value;
                        break;
                }
            }

            _redisDb.KeyExpire(id.ToString(), TimeSpan.FromSeconds(ttl));

            return true; 
        }

        public void AddProductToCash(int id, Product product, int ttl)
        {
            HashEntry[] addingProduct = new HashEntry[]
            {
                new HashEntry("Name", product.Name),
                new HashEntry("Description", product.Description),
                new HashEntry("Price", product.Price.ToString()),
                new HashEntry("Stock", product.Stock)
            };

            _redisDb.HashSet(id.ToString(), addingProduct);
            _redisDb.KeyExpire(id.ToString(), TimeSpan.FromSeconds(ttl));
        }

        public void TryUpdateProductInCash(int id, Product newProduct, int ttl)
        {
            var transaction = _redisDb.CreateTransaction();

            transaction.AddCondition(Condition.KeyExists(id.ToString()));
            transaction.HashSetAsync(id.ToString(), "Name", newProduct.Name);
            transaction.HashSetAsync(id.ToString(), "Description", newProduct.Description);
            transaction.HashSetAsync(id.ToString(), "Price", newProduct.Price.ToString());
            transaction.HashSetAsync(id.ToString(), "Stock", newProduct.Stock);

            try
            {
                transaction.Execute();
            }
            catch
            {
                throw;
            }

            _redisDb.KeyExpire(id.ToString(), TimeSpan.FromSeconds(ttl));
        }

        public void TryDeleteProductInCash(int id)
        {
            _redisDb.KeyDelete(id.ToString());
        }

        public void DecreaseStocks (List<OutputOrderProduct> products, int ttl)
        {
            foreach (OutputOrderProduct product in products)
            {
                var transaction = _redisDb.CreateTransaction();
                int currentStock;

                transaction.AddCondition(Condition.KeyExists(product.ProductId.ToString()));
                HashEntry[] productFromCash = _redisDb.HashGetAll(product.ProductId.ToString());

                if (productFromCash.IsNullOrEmpty())
                    continue;

                currentStock = (int)_redisDb.HashGet(product.ProductId.ToString(), "Stock");
                transaction.HashSetAsync(product.ProductId.ToString(), "Stock", currentStock - product.Quantity);

                try
                {
                    transaction.Execute();
                }
                catch
                {
                    throw;
                }

                _redisDb.KeyExpire(product.ProductId.ToString(), TimeSpan.FromSeconds(ttl));
            }
        }
    }
}
