namespace OrderService.Models
{
    public class Shard
    {
        public int BucketsCount => bucketsIds.Count;

        public Shard(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; private set; }
        public List<int> bucketsIds { get; set; }
    }
}
