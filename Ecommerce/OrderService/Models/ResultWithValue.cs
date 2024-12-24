namespace OrderService.Models
{
    public class ResultWithValue<T>
    {
        public Status Status {  get; set; }
        public string Message { get; set; }
        public T Value { get; set; }
    }
}
