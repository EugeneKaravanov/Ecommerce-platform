namespace ProductService.Models
{
    public class ReservedProductsForOrder
    {
        public int CustomerId { get; set; }
        public ResultWithValue<List<ProductWithId>> products { get; set; }
    }
}
