using Dapper;
using Npgsql;
using OrderService.Models;
using OrderService.Services;
using OrderService.Utilities.Factories;
using ProductServiceGRPC;
using OrderService.Utilities;
using Serilog;

namespace OrderService.Repositories
{
    public class ShardOrderRepository : IOrderRepository
    {
        private readonly ShardConnectionFactory _shardConnectionFactory;
        private readonly ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient _productServiceClient;
        private readonly int _customerIdGlobalIndexBucket = 2;
        private readonly Random _random = new Random();

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

            int bucketId =  _random.Next(0, _shardConnectionFactory.BucketsCount);
            List<OutputOrderItem> orderItems = Mapper.TransferTakeProductResponseToListOutputOrderItem(response);

            decimal totalAmount = 0;

            foreach (OutputOrderItem item in orderItems)
                totalAmount += item.UnitPrice * item.Quantity;

            string sqlStringToInsertAndGetNewOrderId = @$"SELECT NEXTVAL('Bucket{bucketId}.IdCounter');";

            await using var idCounterConnection = _shardConnectionFactory.GetConnectionByBucketId(bucketId);
            await idCounterConnection.OpenAsync();
            int id = await idCounterConnection.QuerySingleAsync<int>(sqlStringToInsertAndGetNewOrderId);

            await using var bucketConnection = _shardConnectionFactory.GetConnectionByOrderId(id, out bucketId);

            string sqlStringForInsertOrderInOrders = @$"INSERT INTO Bucket{bucketId}.Orders (id, customerid, orderdate, totalamount)
                                                        VALUES (@Id, @CustomerId, @Orderdate, @Totalamount)";

            string sqlStringForInsertOrderItemInOrderItems = @$"INSERT INTO Bucket{bucketId}.OrderItems (orderid, productid, quantity, unitprice)
                                                                VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)";

            await bucketConnection.OpenAsync(cancellationToken);
            
            using (var transaction = await bucketConnection.BeginTransactionAsync())
            {
                try
                {
                    await bucketConnection.ExecuteAsync(sqlStringForInsertOrderInOrders, new
                    {
                        Id = id,
                        CustomerId = order.CustomerId,
                        Orderdate = DateTime.Now,
                        Totalamount = totalAmount,
                    });

                    foreach (var item in orderItems)
                        await bucketConnection.ExecuteAsync(sqlStringForInsertOrderItemInOrderItems, new
                        {
                            OrderId = id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        });

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    Log.Logger.Error("При выполнении метода CreateOrderAsync не удалось завершить транзакцию записи в БД!");

                    throw new Exception("При выполнении метода CreateOrderAsync не удалось завершить транзакцию записи в БД!");
                }
            }

            string sqlStringToInsertInCustomerIdGlobalIndex = @$"INSERT INTO Bucket{_customerIdGlobalIndexBucket}.CustomerIdGlobalIndex (Id, CustomerId)
                                                                VALUES (@Id, @CustomerId)";

            await using var customerIdGlobalIndexConnection = _shardConnectionFactory.GetConnectionByBucketId(_customerIdGlobalIndexBucket);
            await customerIdGlobalIndexConnection.OpenAsync();
            await customerIdGlobalIndexConnection.ExecuteAsync(sqlStringToInsertInCustomerIdGlobalIndex, new
            {
                Id = id,
                CustomerId = order.CustomerId,
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

                await using var connection = _shardConnectionFactory.GetConnectionByBucketId(i);
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
            ResultWithValue<List<OutputOrder>> result = new();
            result.Value = new();
            List<OutputOrder> outputOrders = new();
            string sqlStringToGetOrderIdsByCustomer = @$"SELECT Id FROM Bucket{_customerIdGlobalIndexBucket}.CustomerIdGlobalIndex
                                                        WHERE customerid = @CustomerId";

            await using var customerIdGlobalIndexConnection = _shardConnectionFactory.GetConnectionByBucketId(_customerIdGlobalIndexBucket);
            await customerIdGlobalIndexConnection.OpenAsync(cancellationToken);

            var tempOrderIds = await customerIdGlobalIndexConnection.QueryAsync<int>(sqlStringToGetOrderIdsByCustomer, new { CustomerId = customerId });
            List<int> orderIds = tempOrderIds.ToList();
            List<int> bucketIds = _shardConnectionFactory.GetBucketsByOrderIds(orderIds);

            for (int i = 0; i < bucketIds.Count; i++)
            {
                int bucketId = bucketIds[i];
                string sqlStringForGetOrdersByCustomerId = $"SELECT * FROM Bucket{bucketId}.Orders WHERE customerid = @CustomerId";
                string sqlStringForGetOrderItemsById = $"SELECT * FROM Bucket{bucketId}.OrderItems WHERE orderid = @OrderId";

                await using var connection = _shardConnectionFactory.GetConnectionByBucketId(bucketId);
                await connection.OpenAsync(cancellationToken);

                var tempOrders = await connection.QueryAsync<OutputOrder>(sqlStringForGetOrdersByCustomerId, new { CustomerId = customerId });
                List<OutputOrder> orders = tempOrders.ToList();  

                foreach (OutputOrder order in orders)
                {
                    var tempOrderItems = await connection.QueryAsync<OutputOrderItem>(sqlStringForGetOrderItemsById, new { OrderId = order.Id });
                    order.OrderItems = tempOrderItems.ToList();
                }

                foreach (var order in orders)
                    outputOrders.Add(order);
            }

            if (outputOrders.Count == 0)
            {
                result.Status = Models.Status.Failure;
                result.Message = $"Заказы пользователя {customerId} не найдены!";

                return result;
            }

            result.Status = Models.Status.Success;
            result.Value = outputOrders;

            return result;
        }
    }
}
