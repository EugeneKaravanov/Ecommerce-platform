namespace OrderService.Models
{
    public class Shard
    {
        public int BucketsCount => BucketsIds.Count;

        public Shard(string connectionString, List<int> bucketsIds)
        {
            ConnectionString = connectionString;
            BucketsIds = bucketsIds;
        }

        public string ConnectionString { get; private set; }
        public List<int> BucketsIds { get; set; }

        public int GetBucketId(int number)
        {
            return BucketsIds[number];
        }
    }
}
