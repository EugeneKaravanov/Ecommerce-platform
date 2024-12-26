using Npgsql;
using OrderService.Models;

namespace OrderService.Utilities
{
    public class ShardConnectionFactory
    {
        private List<Shard> _shards;
        private int _bucketsCount;

        public ShardConnectionFactory(List<Shard> shards)
        {
            _shards = shards;
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
            int count = 0;

            foreach (Shard shard in _shards)
                count += shard.BucketsCount;

            return count;     
        }

        private Shard GetShardByBucketId(int bucketId)
        {
            Shard outputShard = null;

            foreach (Shard shard in _shards)
            {
                if (shard.bucketsIds.Contains(bucketId))
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
