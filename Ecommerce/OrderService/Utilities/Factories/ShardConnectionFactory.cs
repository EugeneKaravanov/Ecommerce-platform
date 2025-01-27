using Npgsql;
using OrderService.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OrderService.Utilities.Factories
{
    public class ShardConnectionFactory
    {
        private readonly ShardFactory _shardFactory;

        public ShardConnectionFactory(ShardFactory shardFactory)
        {
            _shardFactory = shardFactory;
            BucketsCount = GetBucketsCount();
        }

        public int BucketsCount { get; set; }

        public NpgsqlConnection GetConnectionByOrderId(int orderId, out int bucketId)
        {
            bucketId = GetBucketNumberByOrderId(orderId);

            string connectionString = GetShardByBucketId(bucketId).ConnectionString;

            return new NpgsqlConnection(connectionString);
        }

        public NpgsqlConnection GetConnectionByBucketId(int bucketId)
        {
            return new NpgsqlConnection(GetShardByBucketId(bucketId).ConnectionString);
        }

        public List<int> GetBucketsByOrderIds(List<int> orderIds)
        {
            List<int> buckets = new();

            foreach (int orderId in orderIds)
                buckets.Add(GetBucketNumberByOrderId(orderId));

            return buckets.Distinct().ToList();
        }

        private int GetBucketNumberByOrderId(int orderId)
        {
            return int.Parse(orderId.ToString().Substring(0, 1));
        }

        private int GetBucketsCount()
        {
            List<Shard> shards = _shardFactory.GetAllShards();
            int count = 0;

            foreach (Shard shard in shards)
                count += shard.Buckets.Length;

            return count;
        }

        private Shard GetShardByBucketId(int bucketId)
        {
            List<Shard> shards = _shardFactory.GetAllShards();
            Shard outputShard = null;

            foreach (Shard shard in shards)
            {
                if (Array.Exists(shard.Buckets, id => id == bucketId))
                {
                    outputShard = shard;
                    return outputShard;
                }
            }

            if (outputShard == null)
                throw new IndexOutOfRangeException();

            return outputShard;
        }
    }
}
