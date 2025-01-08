using Dapper;
using Npgsql;
using OrderService.Models;
using OrderService.Services;
using OrderService.Utilities.Factories;
using ProductServiceGRPC;
using System.Threading.RateLimiting;

namespace OrderService.Repositories
{
    public class ShardOrderRepository : IOrderRepository
    {
        private readonly ShardConnectionFactory _shardConnectionFactory;
        private readonly ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient _productServiceClient;
        private readonly int _idCounterBucketId = 1;
        private readonly int _customerIdGlobalIndexBucket = 2;

        public ShardOrderRepository(ShardConnectionFactory shardConnectionFactory, ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient productServiceClient)
        {
            _shardConnectionFactory = shardConnectionFactory;
            _productServiceClient = productServiceClient;
        }

        public async Task<Result> CreateOrderAsync(InputOrder order, CancellationToken cancellationToken = default)
        {
            Result result = new();
            TakeProductsRequest request = Mapper.TransferListInputOrderItemToTakeProductsRequest(order.OrderItems);
            TakeProductsResponse response = await _productServiceClient.TakeProductsAsync(request, cancellationToken: cancellationToken);

            if (response.ResultCase == TakeProductsResponse.ResultOneofCase.NotReceived)
            {
                result.Status = Models.Status.Failure;
                result.Message = response.NotReceived.Message;

                return result;
            }

            List<OutputOrderItem> orderItems = Mapper.TransferTakeProductResponseToListOutputOrderItem(response);
            decimal totalAmount = 0;

            foreach (OutputOrderItem item in orderItems)
                totalAmount += item.UnitPrice * item.Quantity;

            string SqlStringToInsertAndGetNewOrderId = @$"WITH insert_result AS 
                                                        (
                                                            INSERT INTO Bucket{_idCounterBucketId}.IdCounter DEFAULT VALUES
                                                            RETURNING id
                                                        )
                                                        SELECT id FROM insert_result";

            await using var idCounterConnection = _shardConnectionFactory.GetConnectionByBucketId(_idCounterBucketId);
            await idCounterConnection.OpenAsync(cancellationToken);
            int id = await idCounterConnection.QuerySingleAsync<int>(SqlStringToInsertAndGetNewOrderId);

            int bucketId;
            await using var bucketConnection = _shardConnectionFactory.GetConnectionByOrderId(id, out bucketId);

            string sqlStrinForInsertOrderInOrders = @$"WITH insert_result AS 
                                                    (
                                                        INSERT INTO Bucket{bucketId}.Orders (customerid, orderdate, totalamount)
                                                        VALUES (@Customerid, @Orderdate, @Totalamount)
                                                        RETURNING id
                                                    )
                                                    SELECT id FROM insert_result";

            string sqlStringForInsertOrderItemInOrderItems = @$"INSERT INTO Bucket{bucketId}.OrderItems (orderid, productid, quantity, unitprice)
                                                                VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)";

            await bucketConnection.OpenAsync(cancellationToken);

            int orderId = await bucketConnection.QuerySingleAsync<int>(sqlStrinForInsertOrderInOrders, new
            {
                CustomerId = order.CustomerId,
                Orderdate = DateTime.Now,
                Totalamount = totalAmount,
            });

            foreach (var item in orderItems)
                await bucketConnection.ExecuteAsync(sqlStringForInsertOrderItemInOrderItems, new
                {
                    OrderId = orderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });

            result.Status = Models.Status.Success;
            result.Message = "Заказ успешно сформирован!";

            return result;
        }

        public async Task<List<OutputOrder>> GetOrdersAsync(CancellationToken cancellationToken = default)
        {
            List<OutputOrder> outputOrders = new();

            for(int i = 0; i < _shardConnectionFactory.BucketsCount; i++)
            {
                string sqlStringForGetAllOrders = $"SELECT * FROM Bucket{i}.Orders";
                string sqlStringForGetOrderItemsByOrderId = $"SELECT * FROM Bucket{i}.OrderItems WHERE orderid = @OrderId";

                await using var connection = _shardConnectionFactory.GetConnectionByBucketId(_idCounterBucketId);
                await connection.OpenAsync(cancellationToken);

                var tempOrders = await connection.QueryAsync<OutputOrder>(sqlStringForGetAllOrders);
                List<OutputOrder> orders = tempOrders.ToList();

                foreach (OutputOrder order in orders)
                {
                    outputOrders.Add(order);
                    var tempOrderItems = await connection.QueryAsync<OutputOrderItem>(sqlStringForGetOrderItemsByOrderId, new { OrderId = order.Id });
                    order.OrderItems = tempOrderItems.ToList();
                }
            }

            return outputOrders;
        }

        public async Task<ResultWithValue<OutputOrder>> GetOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            ResultWithValue<OutputOrder> result = new();
            int bucketId;

            await using var connection = _shardConnectionFactory.GetConnectionByOrderId(id, out bucketId);

            string sqlStringForGetOrderById = $"SELECT * FROM Bucket{bucketId}.Orders WHERE id = @Id LIMIT 1";
            string sqlStringForGetOrderItemsByOrderId = $"SELECT * FROM Bucket{bucketId}.OrderItems WHERE orderid = @OrderId";

            await connection.OpenAsync(cancellationToken);

            OutputOrder? order = await connection.QuerySingleOrDefaultAsync<OutputOrder>(sqlStringForGetOrderById, new { Id = id });

            if (order == null)
            {
                result.Status = Models.Status.Failure;
                result.Message = $"Заказ с ID {id} отсутствует!";

                return result;
            }

            var tempOrderItems = await connection.QueryAsync<OutputOrderItem>(sqlStringForGetOrderItemsByOrderId, new { OrderId = order.Id });
            order.OrderItems = tempOrderItems.ToList();
            result.Status = Models.Status.Success;
            result.Value = order;

            return result;
        }

        public async Task<ResultWithValue<List<OutputOrder>>> GetOrdersByCustomerAsync(int customerId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
