namespace ProductService.Models.Redis
{
    public class RedisOutputOrderProduct
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public int Stock { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
