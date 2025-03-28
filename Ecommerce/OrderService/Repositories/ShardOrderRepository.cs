﻿using Dapper;
using Npgsql;
using OrderService.Models;
using OrderService.Services;
using OrderService.Utilities.Factories;
using ProductServiceGRPC;
using OrderService.Utilities;
using MassTransit;
using OrderService.Models.Kafka.KafkaMessages;
using OrderService.Models.Kafka.KafkaDto;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace OrderService.Repositories
{
    public class ShardOrderRepository : IOrderRepository
    {
        private readonly ShardConnectionFactory _shardConnectionFactory;
        private readonly ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient _productServiceClient;
        private readonly int _customerIdGlobalIndexBucket = 2;
        private readonly Random _random = new Random();
        private readonly ITopicProducer<OrderCreated> _producer;
        private readonly RedisController _redis;

        public ShardOrderRepository(ShardConnectionFactory shardConnectionFactory, ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient productServiceClient, ITopicProducer<OrderCreated> producer, RedisController redis)
        {
            _shardConnectionFactory = shardConnectionFactory;
            _productServiceClient = productServiceClient;
            _producer = producer;
            _redis = redis;
        }

        public async Task<ResultWithValue<OrderFormed>> CreateOrderAsync(ProductsReserved productsReserved, CancellationToken cancellationToken = default)
        {
            ResultWithValue<OrderFormed> result = new();
            result.Value = new();
            int bucketId = _random.Next(0, _shardConnectionFactory.BucketsCount);

            decimal totalAmount = 0;

            foreach (OutputOrderItemKafkaDto item in productsReserved.OrderProducts)
                totalAmount += item.UnitPrice * item.Quantity;

            var orderDate = DateTime.Now;

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
                        CustomerId = productsReserved.CustomerId,
                        Orderdate = orderDate,
                        Totalamount = totalAmount,
                    });

                    foreach (var item in productsReserved.OrderProducts)
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

                    throw;
                }
            }

            string sqlStringToInsertInCustomerIdGlobalIndex = @$"INSERT INTO Bucket{_customerIdGlobalIndexBucket}.CustomerIdGlobalIndex (Id, CustomerId)
                                                                VALUES (@Id, @CustomerId)";

            await using var customerIdGlobalIndexConnection = _shardConnectionFactory.GetConnectionByBucketId(_customerIdGlobalIndexBucket);
            await customerIdGlobalIndexConnection.OpenAsync();
            await customerIdGlobalIndexConnection.ExecuteAsync(sqlStringToInsertInCustomerIdGlobalIndex, new
            {
                Id = id,
                CustomerId = productsReserved.CustomerId,
            });

            OutputOrder order = Mapper.TransferIdAndProductsReservedAndTotalAmmountAndOrderDateToOutputOrder(id, productsReserved, totalAmount, orderDate);

            await _redis.AddOrderToCache(id, order);
            result.Value = Mapper.TransferOutputOrderToOrderFormed(order);
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
            ResultWithValue<OutputOrder> redisResult = await _redis.TryGetOrderFromCache(id);
            OutputOrder order = new();
            int bucketId;

            if (redisResult.Status == Models.Status.Success)
            {
                result.Status = Models.Status.Success;
                result.Value = redisResult.Value;

                return result;
            }

            await using var connection = _shardConnectionFactory.GetConnectionByOrderId(id, out bucketId);

            string sqlStringForGetOrderById = $"SELECT * FROM Bucket{bucketId}.Orders WHERE id = @Id LIMIT 1";
            string sqlStringForGetOrderItemsByOrderId = $"SELECT * FROM Bucket{bucketId}.OrderItems WHERE orderid = @OrderId";

            await connection.OpenAsync(cancellationToken);

            order = await connection.QuerySingleOrDefaultAsync<OutputOrder>(sqlStringForGetOrderById, new { Id = id });

            if (order == null)
            {
                result.Status = Models.Status.Failure;
                result.Message = $"Заказ с ID {id} отсутствует!";

                return result;
            }

            var tempOrderItems = await connection.QueryAsync<OutputOrderItem>(sqlStringForGetOrderItemsByOrderId, new { OrderId = order.Id });

            order.OrderItems = tempOrderItems.ToList();
            await _redis.AddOrderToCache(id, order);
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
