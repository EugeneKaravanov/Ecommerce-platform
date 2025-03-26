using ProductServiceGRPC;
using GatewayService.Models;
using ProductService.Models;
using ProductService;
using OrderServiceGRPC;

namespace GatewayService.Utilities
{
    internal class Mapper
    {
        internal static ProductGRPC TransferProductDtoToProdutctGRPC(ProductDto productDto)
        {
            ProductGRPC productInfo = new ProductGRPC();

            productInfo.Name = productDto.Name;
            productInfo.Description = productDto.Description;
            productInfo.Price = MoneyConverter.ConvertDecimalToMoney(productDto.Price);
            productInfo.Stock = productDto.Stock;

            return productInfo;
        }

        internal static ProductWithIdDto TransferProductGRPCToProdutctWithIdDto(ProductGRPC productGrpc)
        {
            ProductWithIdDto productWithIdDto = new ProductWithIdDto();

            productWithIdDto.Id = productGrpc.Id;
            productWithIdDto.Name = productGrpc.Name;
            productWithIdDto.Description = productGrpc.Description;
            productWithIdDto.Price = MoneyConverter.ConvertMoneyToDecimal(productGrpc.Price);
            productWithIdDto.Stock = productGrpc.Stock;

            return productWithIdDto;
        }

        internal static ProductGRPC TransferProductDtoAndIdToProductGRPC(int id, ProductDto productDto)
        {
            ProductGRPC productGrpc = new ProductGRPC();

            productGrpc.Id = id;
            productGrpc.Name = productDto.Name;
            productGrpc.Description = productDto.Description;
            productGrpc.Price = MoneyConverter.ConvertDecimalToMoney(productDto.Price);
            productGrpc.Stock = productDto.Stock;

            return productGrpc;
        }

        internal static GetProductsRequest TransferGetProductsRequestDtoToGetProductsRequest(GetProductsRequestDto getProductsRequestDto)
        {
            GetProductsRequest request = new GetProductsRequest
            {
                ElementsOnPageCount = getProductsRequestDto.ElementsOnPageCount,
                ChoosenPageNumber = getProductsRequestDto.ChoosenPageNumber,
                NameFilter = getProductsRequestDto.NameFilter,
                MinPriceFilter = (uint?)getProductsRequestDto.MinPriceFilter,
                MaxPriceFilter = (uint?)getProductsRequestDto.MaxPriceFilter,
                SortArgument = getProductsRequestDto.SortArgument,
                IsReverseSort = getProductsRequestDto.IsReverseSort,
            };

            return request;
        }

        internal static PageDto<ProductWithIdDto> TransferPageGRPCToPageDto(PageGRPC pageGrpc)
        {
            PageDto<ProductWithIdDto> pageDto = new PageDto<ProductWithIdDto>();

            pageDto.TotalElementcCount = pageGrpc.TotalElementsCount;
            pageDto.TotalPagesCount = pageGrpc.TotalPagesCount;
            pageDto.ChoosenPageNumber = pageGrpc.ChoosenPageNumber;
            pageDto.ElementOnPageCount = pageGrpc.ElementsOnPageCount;
            pageDto.Products = new List<ProductWithIdDto>();
            
            foreach (ProductGRPC productWithIdGRPC in pageGrpc.Products)
                pageDto.Products.Add(TransferProductGRPCToProdutctWithIdDto(productWithIdGRPC));

            return pageDto;
        }

        internal static OutputOrderDto TransferOutputOrderGRPCToOutputOrderDto(OutputOrderGRPC outputOrderGRPC)
        {
            OutputOrderDto outputOrderDto = new();
            outputOrderDto.OrderItems = new();

            outputOrderDto.Id = outputOrderGRPC.Id;
            outputOrderDto.CustomerId = outputOrderGRPC.CustomerId;
            outputOrderDto.OrderDate = outputOrderGRPC.DateTime.ToDateTime();
            outputOrderDto.TotalAmount = MoneyConverter.ConvertMoneyToDecimal(outputOrderGRPC.TotalAmount);

            foreach (OutputOrderItemGRPC outputOrderItemGRPC in outputOrderGRPC.Items)
                outputOrderDto.OrderItems.Add(TransferOutputOrderItemGRPCToOutputOrderItemDto(outputOrderItemGRPC));

            return outputOrderDto;
        }

        internal static OutputOrderItemDto TransferOutputOrderItemGRPCToOutputOrderItemDto(OutputOrderItemGRPC outputOrderItemGRPC)
        {
            OutputOrderItemDto outputOrderItemDto = new();

            outputOrderItemDto.ProductId = outputOrderItemGRPC.ProductId;
            outputOrderItemDto.Quantity = outputOrderItemGRPC.Quantity;
            outputOrderItemDto.UnitPrice = MoneyConverter.ConvertMoneyToDecimal(outputOrderItemGRPC.UnitPrice);

            return outputOrderItemDto;
        }

        internal static List<OutputOrderDto> TransferGetOrdersByCustomerResponseToListOutputOrderDto(GetOrdersByCustomerResponse getOrdersByCustomerResponse)
        {
            List<OutputOrderDto> orders = new();

            foreach (OutputOrderGRPC orderGRPC in getOrdersByCustomerResponse.Found.Orders)
                orders.Add(TransferOutputOrderGRPCToOutputOrderDto(orderGRPC));

            return orders;
        }

        internal static List<OutputOrderDto> TransferGetOrdersResponseToListOutputOrderDto(GetOrdersResponse getOrdersResponse)
        {
            List<OutputOrderDto> orders = new();

            foreach (OutputOrderGRPC orderGRPC in getOrdersResponse.Orders)
                orders.Add(TransferOutputOrderGRPCToOutputOrderDto(orderGRPC));

            return orders;
        }

        internal static CreateOrderRequest TransferInputOrderDtoToCreateOrderRequest(InputOrderDto inputOrderDto)
        {
            CreateOrderRequest request = new();
            request.Order = new();

            request.Order.CustomerId = inputOrderDto.CustomerId;
            
            foreach (InputOrderItemDto inputOrderItemDto in inputOrderDto.OrderItems)
                request.Order.Items.Add(TransferInputOrderItemDtoToInputOrderItemGRPC(inputOrderItemDto));

            return request;
        }

        internal static InputOrderItemGRPC TransferInputOrderItemDtoToInputOrderItemGRPC(InputOrderItemDto inputOrderItemDto)
        {

            InputOrderItemGRPC inputOrderItemGRPC = new();

            inputOrderItemGRPC.ProductId = inputOrderItemDto.ProductId;
            inputOrderItemGRPC.Quantity = inputOrderItemDto.Quantity;

            return inputOrderItemGRPC;
        }
    }
}
