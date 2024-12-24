namespace GatewayService.Models
{
    public class InputOrderDto
    {
        public int CustomerId { get; set; }
        public List<InputOrderItemDto> OrderItems { get; set; }
    }
}
