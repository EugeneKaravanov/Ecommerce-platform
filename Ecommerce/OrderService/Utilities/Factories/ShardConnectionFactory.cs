using Npgsql;
using OrderService.Models;

namespace OrderService.Utilities.Factories
{
    public class ShardConnectionFactory
    {
        private int _bucketsCount;
        private readonly ShardFactory _shardFactory;

        public ShardConnectionFactory(ShardFactory shardFactory)
        {
            _shardFactory = shardFactory;
            _bucketsCount = GetBucketsCount();
        }

        public NpgsqlConnection GetConnectionByOrderId(int orderId, out string bucketName)
        {
            int bucketId = GetBucketNumberByOrderId(orderId);
            string connectionString = GetShardByBucketId(bucketId).ConnectionString;

            bucketName = "bucket-" + bucketId;

            return new NpgsqlConnection(connectionString);
        }

        private int GetBucketNumberByOrderId(int orderId)
        {
            return orderId % _bucketsCount;
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
