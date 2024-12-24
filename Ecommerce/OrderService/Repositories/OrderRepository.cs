using Dapper;
using Npgsql;
using OrderService.Models;
using OrderService.Services;
using ProductServiceGRPC;

namespace OrderService.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _сonnectionString;
        private readonly ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient _productServiceClient;

        public OrderRepository(string сonnectionString, ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient productServiceClient)
        {
            _сonnectionString = сonnectionString;
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

            string sqlStrinForInsertOrderInOrders = @"WITH insert_result AS 
                                                    (
                                                        INSERT INTO Orders (customerid, orderdate, totalamount)
                                                        VALUES (@Customerid, @Orderdate, @Totalamount)
                                                        RETURNING id
                                                    )
                                                    SELECT id FROM insert_result";

            string sqlStringForInsertOrderItemInOrderItems = @"INSERT INTO OrderItems (orderid, productid, quantity, unitprice)
                                                                VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)";

            await using var connection = new NpgsqlConnection(_сonnectionString);

            await connection.OpenAsync();

            int orderId = await connection.QuerySingleAsync<int>(sqlStrinForInsertOrderInOrders, new
            {
                CustomerId = order.CustomerId,
                Orderdate = DateTime.Now,
                Totalamount = totalAmount,
            });

            foreach (var item in orderItems)
                await connection.ExecuteAsync(sqlStringForInsertOrderItemInOrderItems, new
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
            string sqlStringForGetAllOrders = "SELECT * FROM Orders";
            string sqlStringForGetOrderItemsByOrderId = "SELECT * FROM OrderItems WHERE orderid = @OrderId";
            await using var connection = new NpgsqlConnection(_сonnectionString);

            await connection.OpenAsync(cancellationToken);

            var tempOrders = await connection.QueryAsync<OutputOrder>(sqlStringForGetAllOrders);
            List<OutputOrder> orders = tempOrders.ToList();

            foreach (OutputOrder order in orders)
            {
                var tempOrderItems =  await connection.QueryAsync<OutputOrderItem>(sqlStringForGetOrderItemsByOrderId, new { OrderId = order.Id});
                order.OrderItems = tempOrderItems.ToList();
            }

            return orders;
        }

        public async Task<ResultWithValue<OutputOrder>> GetOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            ResultWithValue<OutputOrder> result = new();
            string sqlStringForGetOrderById = "SELECT * FROM Orders WHERE id = @Id LIMIT 1";
            string sqlStringForGetOrderItemsByOrderId = "SELECT * FROM OrderItems WHERE orderid = @OrderId";
            await using var connection = new NpgsqlConnection(_сonnectionString);

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
            string sqlStringForGetOrdersByCustomerId = "SELECT * FROM Orders WHERE customerid = @CustomerId";
            string sqlStringForGetOrderItemsById = "SELECT * FROM OrderItems WHERE orderid = @OrderId";
            await using var connection = new NpgsqlConnection(_сonnectionString);

            await connection.OpenAsync(cancellationToken);
            var tempOrders = await connection.QueryAsync<OutputOrder>(sqlStringForGetOrdersByCustomerId, new { CustomerId = customerId});
            List<OutputOrder> orders = tempOrders.ToList();

            if (orders.Count == 0)
            {
                result.Status = Models.Status.Failure;
                result.Message = $"Заказы пользователя {customerId} не найдены!";

                return result;
            }

            foreach (OutputOrder order in orders)
            {
                var tempOrderItems = await connection.QueryAsync<OutputOrderItem>(sqlStringForGetOrderItemsById, new { OrderId = order.Id });
                order.OrderItems = tempOrderItems.ToList();
            }

            result.Status = Models.Status.Success;
            result.Value = orders;

            return result;
        }
    }
}