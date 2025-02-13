using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using OrderServiceGRPC;
using OrderService.Repositories;
using OrderService.Models;
using static OrderServiceGRPC.OrderServiceGRPC;
using OperationStatusResponse = OrderServiceGRPC.OperationStatusResponse;
using ProductServiceGRPC;
using OrderService.Validators;
using OrderService.Utilities;
using System.Threading;
using MassTransit;
using OrderService.Models.Kafka.KafkaMessages;

namespace OrderService.Services
{
    public class OrderGRPCService : OrderServiceGRPCBase
    {
        private readonly IOrderRepository _repository;
        private readonly OrderValidator _validator;
        private readonly ITopicProducer<OrderCreated> _producer;

        public OrderGRPCService(IOrderRepository repository, OrderValidator validator, ITopicProducer<OrderCreated> producer)
        {
            _repository = repository;
            _validator = validator;
            _producer = producer;
        }

        public override async Task<OrderServiceGRPC.OperationStatusResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
        {
            OperationStatusResponse response = new();
            OrderCreated orderCreated = Mapper.TransferCreateOrderRequestToOrderCreated(request);

            await _producer.Produce(orderCreated, context.CancellationToken);

            response.Status = Mapper.TransferResultStatusToResponseStatus(Models.Status.Success);
            response.Message = "Заказ принят в обработку!";

            return response;
        }

        public override async Task<GetOrdersResponse> GetOrders(Empty request, ServerCallContext context)
        {
            return Mapper.TransferListOutputOrderToGetOrdersResponse(await _repository.GetOrdersAsync(context.CancellationToken));
        }

        public override async Task<GetOrderResponse> GetOrder(GetOrderRequest request, ServerCallContext context)
        {
            GetOrderResponse getOrderResponse = new();
            ResultWithValue<OutputOrder> result = await _repository.GetOrderAsync(request.Id, context.CancellationToken);

            if (result.Status == Models.Status.Failure)
            {
                GetOrderResponse.Types.OrderNotFound orderNotFound = new();

                orderNotFound.Message = result.Message;
                getOrderResponse.NotFound = orderNotFound;
                
                return getOrderResponse;
            }

            GetOrderResponse.Types.OrderFound orderFound = new();
            orderFound.Order = Mapper.TransferOutputOrderToOutputOrderGRPC(result.Value);
            getOrderResponse.Found = orderFound;

            return getOrderResponse;
        }

        public override async Task<GetOrdersByCustomerResponse> GerOrdersByCustomer(GetOrderByCustomerRequest request, ServerCallContext context)
        {
            GetOrdersByCustomerResponse getOrdersByCustomerResponse = new();
            ResultWithValue<List<OutputOrder>> result = await _repository.GetOrdersByCustomerAsync(request.CustomerId, context.CancellationToken);

            if (result.Status == Models.Status.Failure)
            {
                GetOrdersByCustomerResponse.Types.OrdersNotFound orderNotFound = new();

                orderNotFound.Message = result.Message;
                getOrdersByCustomerResponse.NotFound = orderNotFound;

                return getOrdersByCustomerResponse;
            }

            GetOrdersByCustomerResponse.Types.OrdersFound ordersFound = new();
            
            foreach (var order in result.Value)
                ordersFound.Orders.Add(Mapper.TransferOutputOrderToOutputOrderGRPC(order));

            getOrdersByCustomerResponse.Found = ordersFound;

            return getOrdersByCustomerResponse;
        }
    }
}
