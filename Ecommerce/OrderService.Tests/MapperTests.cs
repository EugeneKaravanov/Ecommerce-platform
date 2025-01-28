using OrderService;

namespace OrderService.Tests
{
    public class MapperTests
    {
        [Fact]
        public void MapperTest()
        {
            var inputOrderItemGRPC = new OrderServiceGRPC.InputOrderItemGRPC
            {
                ProductId = 1,
                Quantity = 5
            };

            var result = OrderService.Utilities.Mapper.TransferInputOrderItemGRPCToInputOrderItem(inputOrderItemGRPC);

            Assert.NotNull(result);
            Assert.Equal(inputOrderItemGRPC.ProductId, result.ProductId);
            Assert.Equal(inputOrderItemGRPC.Quantity, result.Quantity);
        }
    }
}