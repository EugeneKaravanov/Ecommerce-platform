namespace GatewayService.Models
{
    public class GetProductsRequestDto
    {
        public int ElementsOnPageCount { get; set; }
        public int ChoosenPageNumber { get; set; }
        public string NameFilter { get; set; }
        public int MinPriceFilter { get; set; }
        public int MaxPriceFilter { get; set; }
        public string SortArgument { get; set; }
        public bool IsReverseSort { get; set; }
    }
}
