using ProductService.Models;
using ProductService.Models.Kafka.KafkaMessages;
using ProductServiceGRPC;

namespace ProductService.Repositories
{
    public interface IProductRepository
    {
        public Task<Page<ProductWithId>> GetProductsAsync(GetProductsRequest getProductsRequest, CancellationToken cancellationToken = default);
        public Task<ResultWithValue<ProductWithId>> GetProductAsync(int id, CancellationToken cancellationToken = default);
        public Task<Result> CreateProductAsync(Product product, CancellationToken cancellationToken = default);
        public Task<Result> UpdateProductAsync(int id, Product product, CancellationToken cancellationToken = default);
        public Task<Result> DeleteProductAsync(int id, CancellationToken cancellationToken = default);
        public Task<ResultWithValue<List<OutputOrderProduct>>> TakeProducts(OrderCreated order, CancellationToken cancellationToken = default);
    }
}
