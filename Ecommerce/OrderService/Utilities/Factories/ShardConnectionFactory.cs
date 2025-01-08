using Npgsql;
using OrderService.Models;

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

        private int GetBucketNumberByOrderId(int orderId)
        {
            return orderId % BucketsCount;
        }

        private int GetBucketsCount()
        {
            List<Shard> shards = _shardFactory.GetAllShards();
            int count = 0;

            foreach (Shard shard in shards)
                count += shard.BucketsCount;

            return count;
        }

        private Shard GetShardByBucketId(int bucketId)
        {
            List<Shard> shards = _shardFactory.GetAllShards();
            Shard outputShard = null;

            foreach (Shard shard in shards)
            {
                if (shard.BucketsIds.Contains(bucketId))
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
