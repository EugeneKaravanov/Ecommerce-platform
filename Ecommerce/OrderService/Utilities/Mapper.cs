using OrderService.Models;
using OrderService.Models.Kafka.KafkaDto;
using OrderService.Models.Kafka.KafkaMessages;
using OrderService.Utilities;
using OrderServiceGRPC;
using ProductServiceGRPC;

namespace OrderService.Utilities
{
    public class Mapper
    {
        public static TakeProductsRequest TransferListInputOrderItemToTakeProductsRequest(List<InputOrderItem> orderItems)
        {
            TakeProductsRequest takeProductsRequest = new();

            foreach (InputOrderItem item in orderItems)
            {
                InputTakeProductGRPC inputTakeProductGRPC = new();

                inputTakeProductGRPC.Id = item.ProductId;
                inputTakeProductGRPC.Quantity = item.Quantity;
                takeProductsRequest.ProductOrders.Add(inputTakeProductGRPC);
            }

            return takeProductsRequest;
        }

        public static List<OutputOrderItem> TransferTakeProductResponseToListOutputOrderItem(TakeProductsResponse takeProductsResponse)
        {
            List<OutputOrderItem> orderItems = new();

            foreach (OutputTakeProductGRPC product in takeProductsResponse.Received.ProductOrders)
            {
                OutputOrderItem outputOrderItem = new();

                outputOrderItem.ProductId = product.Id;
                outputOrderItem.Quantity = product.Quantity;
                outputOrderItem.UnitPrice = MoneyConverter.ConvertMoneyToDecimal(product.UnitPrice);

                orderItems.Add(outputOrderItem);
            }

            return orderItems;
        }

        public static GetOrdersResponse TransferListOutputOrderToGetOrdersResponse(List<OutputOrder> outputOrders)
        {
            GetOrdersResponse getOrdersResponse = new();

            foreach (OutputOrder outputOrder in outputOrders)
                getOrdersResponse.Orders.Add(TransferOutputOrderToOutputOrderGRPC(outputOrder));

            return getOrdersResponse;
        }

        public static OutputOrderGRPC TransferOutputOrderToOutputOrderGRPC(OutputOrder outputOrder)
        {
            OutputOrderGRPC outputOrderGRPC = new();

            outputOrderGRPC.Id = outputOrder.Id;
            outputOrderGRPC.CustomerId = outputOrder.CustomerId;
            outputOrderGRPC.DateTime = TimeConverter.ConvertDateTimeToTimeStapm(outputOrder.OrderDate);
            outputOrderGRPC.TotalAmount = MoneyConverter.ConvertDecimalToMoney(outputOrder.TotalAmount);

            foreach (OutputOrderItem outputOrderItem in outputOrder.OrderItems)
                outputOrderGRPC.Items.Add(TransferOutputOrderItemToOutputOrderItemGRPC(outputOrderItem));

            return outputOrderGRPC;
        }

        public static OutputOrderItemGRPC TransferOutputOrderItemToOutputOrderItemGRPC(OutputOrderItem outputOrderItem)
        {
            OutputOrderItemGRPC outputOrderItemGRPC = new();

            outputOrderItemGRPC.ProductId = outputOrderItem.ProductId;
            outputOrderItemGRPC.Quantity = outputOrderItem.Quantity;
            outputOrderItemGRPC.UnitPrice = MoneyConverter.ConvertDecimalToMoney(outputOrderItem.UnitPrice);

            return outputOrderItemGRPC;
        }

        public static InputOrder TransferCreateOrderRequestToInputOrder(CreateOrderRequest createOrderRequest)
        {
            InputOrder inputOrder = new();
            inputOrder.OrderItems = new();

            inputOrder.CustomerId = createOrderRequest.Order.CustomerId;

            foreach (InputOrderItemGRPC inputOrderItemGRPC in createOrderRequest.Order.Items)
                inputOrder.OrderItems.Add(TransferInputOrderItemGRPCToInputOrderItem(inputOrderItemGRPC));

            return inputOrder;
        }

        public static InputOrderItem TransferInputOrderItemGRPCToInputOrderItem(InputOrderItemGRPC inputOrderItemGRPC)
        {
            InputOrderItem inputOrderItem = new();

            inputOrderItem.ProductId = inputOrderItemGRPC.ProductId;
            inputOrderItem.Quantity = inputOrderItemGRPC.Quantity;

            return inputOrderItem;
        }

        public static OrderCreated TransferCreateOrderRequestToOrderCreated(CreateOrderRequest createOrderRequest)
        {
            OrderCreated orderCreated = new();
            
            orderCreated.OrderProducts = new();
            orderCreated.CustomerId = createOrderRequest.Order.CustomerId;

            foreach (InputOrderItemGRPC inputOrderItemGRPC in createOrderRequest.Order.Items)
                orderCreated.OrderProducts.Add(TransferInputOrderItemGRPCToInputOrderItemKafkaDto(inputOrderItemGRPC));

            return orderCreated;
        }

        public static InputOrderItemKafkaDto TransferInputOrderItemGRPCToInputOrderItemKafkaDto(InputOrderItemGRPC inputOrderItemGRPC)
        {
            InputOrderItemKafkaDto inputOrderItem = new();

            inputOrderItem.ProductId = inputOrderItemGRPC.ProductId;
            inputOrderItem.Quantity = inputOrderItemGRPC.Quantity;

            return inputOrderItem;
        }


        public static InputOrder TransferProductsReservedToInputOrder(ProductsReserved productsReserved)
        {
            InputOrder inputOrder = new();

            //inputOrder.CustomerId = productsReserved.CustomerId;
            //inputOrder.OrderItems = productsReserved.OrderProducts;

            //foreach (OutputOrderItem = )

            return inputOrder;
        }

        public static OrderServiceGRPC.Status TransferResultStatusToResponseStatus(Models.Status status)
        {
            switch (status)
            {
                case Models.Status.Success:
                    return OrderServiceGRPC.Status.Success;

                case Models.Status.NotFound:
                    return OrderServiceGRPC.Status.NotFound;

                default:
                    return OrderServiceGRPC.Status.Failure;
            }
        }
    }
}
