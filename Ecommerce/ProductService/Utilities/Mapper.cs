using ProductServiceGRPC;
using ProductService.Models;
using ProductService.Models.Kafka.KafkaMessages;
using ProductService.Models.Kafka.KafkaDto;

namespace ProductService.Utilities
{
    internal class Mapper
    {
        internal static ProductGRPC TransferProductAndIdToProductGRPC(int id, Product product)
        {
            ProductGRPC productGrpc = new ProductGRPC();

            productGrpc.Id = id;
            productGrpc.Name = product.Name;
            productGrpc.Description = product.Description;
            productGrpc.Price = MoneyConverter.ConvertDecimalToMoney(product.Price);
            productGrpc.Stock = product.Stock;

            return productGrpc;
        }

        internal static Product TransferProductGRPCToProductAndId(ProductGRPC productGrpc, out int id)
        {
            Product product = new Product();

            id = productGrpc.Id;
            product.Name = productGrpc.Name;
            product.Description = productGrpc.Description;
            product.Price = MoneyConverter.ConvertMoneyToDecimal(productGrpc.Price);
            product.Stock = productGrpc.Stock;

            return product;
        }

        internal static ProductWithId TansferProductAndIdToProductWithId(int id, Product product)
        {
            ProductWithId productWithId = new ProductWithId();

            productWithId.Id = id;
            productWithId.Name = product.Name;
            productWithId.Description = product.Description;
            productWithId.Price = product.Price;
            productWithId.Stock = product.Stock;

            return productWithId;
        }

        internal static ProductGRPC TransferProductWithIdToProductGrpc(ProductWithId productWithId)
        {
            ProductGRPC productGrpc = new ProductGRPC();

            productGrpc.Id = productWithId.Id;
            productGrpc.Name = productWithId.Name;
            productGrpc.Description = productWithId.Description;
            productGrpc.Price = MoneyConverter.ConvertDecimalToMoney(productWithId.Price);
            productGrpc.Stock = productWithId.Stock;

            return productGrpc;
        }

        internal static PageGRPC TrasferPageToPageGRPC(Page<ProductWithId> page)
        {
            PageGRPC pageGRPC = new PageGRPC();

            pageGRPC.TotalElementsCount = page.TotalElementcCount;
            pageGRPC.TotalPagesCount = page.TotalPagesCount;
            pageGRPC.ChoosenPageNumber = page.ChoosenPageNumber;
            pageGRPC.ElementsOnPageCount = page.ElementOnPageCount;

            foreach (ProductWithId product in page.Products)
                pageGRPC.Products.Add(TransferProductWithIdToProductGrpc(product));

            return pageGRPC;
        }

        internal static InputOrderProduct TransferInputTakeProductsGRPCToIncomingOrderProduct(InputTakeProductGRPC inputTakeProductsGRPC)
        {
            InputOrderProduct inputOrderProduct = new();

            inputOrderProduct.ProductId = inputTakeProductsGRPC.Id;
            inputOrderProduct.Quantity = inputTakeProductsGRPC.Quantity;

            return inputOrderProduct;
        }

        internal static List<InputOrderProduct> TransferTakeProductsRequestToIncomingOrderProductList(TakeProductsRequest request)
        {
            List<InputOrderProduct> inputOrderProducts = new();

            foreach (InputTakeProductGRPC product in request.ProductOrders)
                inputOrderProducts.Add(TransferInputTakeProductsGRPCToIncomingOrderProduct(product));

            return inputOrderProducts;
        }

        internal static TakeProductsResponse.Types.ProductsReceived TransferListOutputOrderProductToProductsrReceived(List<OutputOrderProduct> orderProducts)
        {
            TakeProductsResponse.Types.ProductsReceived productsReceived = new();

            foreach (OutputOrderProduct orderProduct in orderProducts)
                productsReceived.ProductOrders.Add(TansferOutputOrderProductToOutputTakeProductGRPC(orderProduct));

            return productsReceived;
        }

        internal static OutputTakeProductGRPC TansferOutputOrderProductToOutputTakeProductGRPC(OutputOrderProduct orderProduct)
        {
            OutputTakeProductGRPC outputTakeProductGRPC = new();

            outputTakeProductGRPC.Id = orderProduct.ProductId;
            outputTakeProductGRPC.Quantity = orderProduct.Quantity;
            outputTakeProductGRPC.UnitPrice = MoneyConverter.ConvertDecimalToMoney(orderProduct.UnitPrice);

            return outputTakeProductGRPC;
        }

        internal static ProductsReserved TransferTakeProductsResultAndOrderCreatedToProductsReserved(ResultWithValue<List<OutputOrderProduct>> result, OrderCreated orderCreated)
        {
            ProductsReserved productsReserved = new();

            productsReserved.CustomerId = orderCreated.CustomerId;
            productsReserved.Message = result.Message;
            productsReserved.Status = result.Status;
            productsReserved.OrderProducts = new();

            foreach (OutputOrderProduct outputOrderProduct in result.Value)
                productsReserved.OrderProducts.Add(TransferOutputOrderProductToOutputOrderItemKafkaDto(outputOrderProduct));

            return productsReserved;
        }

        internal static OutputOrderItemKafkaDto TransferOutputOrderProductToOutputOrderItemKafkaDto(OutputOrderProduct product)
        {
            OutputOrderItemKafkaDto outputOrderItemKafkaDto = new();

            outputOrderItemKafkaDto.ProductId = product.ProductId;
            outputOrderItemKafkaDto.Quantity = product.Quantity;
            outputOrderItemKafkaDto.UnitPrice = product.UnitPrice;

            return outputOrderItemKafkaDto;
        }

        internal static Product TransferProductWithIdToProduct(ProductWithId productWithId)
        {
            Product product = new();

            product.Name = productWithId.Name;
            product.Description = productWithId.Description;
            product.Price = product.Price;
            product.Stock = product.Stock;

            return product;
        }

        internal static ProductServiceGRPC.Status TransferResultStatusToResponseStatus(Models.Status status)
        {
            switch (status)
            {
                case Models.Status.Success:
                    return ProductServiceGRPC.Status.Success;

                case Models.Status.NotFound:
                    return ProductServiceGRPC.Status.NotFound;

                default:
                    return ProductServiceGRPC.Status.Failure;
            }
        }
    }
}
