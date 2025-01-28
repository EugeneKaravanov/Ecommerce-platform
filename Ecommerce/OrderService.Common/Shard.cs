namespace OrderService.Models
{
    public class Shard
    {
        public string ConnectionString { get; set; }
        public int[] Buckets { get; set; }

        public int GetBucketId(int number)
        {
            return Buckets[number];
        }
    }
}
