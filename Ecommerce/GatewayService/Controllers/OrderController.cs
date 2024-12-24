using GatewayService.Models;
using GatewayService.Utilities;
using Microsoft.AspNetCore.Mvc;
using OrderServiceGRPC;

namespace GatewayService.Controllers
{
    public class OrderController : Controller
    {
        private readonly OrderServiceGRPC.OrderServiceGRPC.OrderServiceGRPCClient _orderServiceClient;

        public OrderController(OrderServiceGRPC.OrderServiceGRPC.OrderServiceGRPCClient orderServiceClient)
        {
            _orderServiceClient = orderServiceClient;
        }

        [HttpPost("orders")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrder([FromBody] InputOrderDto inputOrderDto, CancellationToken cancellationToken)
        {
            CreateOrderRequest request = Mapper.TransferInputOrderDtoToCreateOrderRequest(inputOrderDto);
            OperationStatusResponse response = await _orderServiceClient.CreateOrderAsync(request, cancellationToken: cancellationToken);

            if (response.Status == OrderServiceGRPC.Status.Success)
                return Ok(response.Message);
            else
                return BadRequest(response.Message);
        }

        [HttpGet("orders")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<List<OutputOrderDto>> GetOrders(CancellationToken cancellationToken)
        {
            Google.Protobuf.WellKnownTypes.Empty request = new();
            GetOrdersResponse gerOrdersResponse = await _orderServiceClient.GetOrdersAsync(request, cancellationToken: cancellationToken);

            return Mapper.TransferGetOrdersResponseToListOutputOrderDto(gerOrdersResponse);
        }

        [HttpGet("orders/{id}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrder(int id, CancellationToken cancellationToken)
        {
            GetOrderRequest request = new GetOrderRequest { Id = id };
            GetOrderResponse response = await _orderServiceClient.GetOrderAsync(request, cancellationToken: cancellationToken);

            if (response.ResultCase == GetOrderResponse.ResultOneofCase.NotFound)
                return NotFound(response.NotFound.Message);

            OutputOrderDto order = Mapper.TransferOutputOrderGRPCToOutputOrderDto(response.Found.Order);

            return Ok(order);
        }

        [HttpGet("orders/by-customer/{customerId}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrdersByCustomer(int customerId, CancellationToken cancellationToken)
        {
            GetOrderByCustomerRequest request = new GetOrderByCustomerRequest { CustomerId = customerId };
            GetOrdersByCustomerResponse response = await _orderServiceClient.GerOrdersByCustomerAsync(request, cancellationToken: cancellationToken);

            if (response.ResultCase == GetOrdersByCustomerResponse.ResultOneofCase.NotFound)
                return NotFound(response.NotFound.Message);

            List<OutputOrderDto> orders = Mapper.TransferGetOrdersByCustomerResponseToListOutputOrderDto(response);

            return Ok(orders);
        }
    }
}
