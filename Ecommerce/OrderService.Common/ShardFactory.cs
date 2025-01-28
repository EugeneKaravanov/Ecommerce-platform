using OrderService.Models;

namespace OrderService.Utilities.Factories
{
    public class ShardFactory
    {
        private readonly List<Shard> _shards;

        public ShardFactory(List<Shard> shards) 
        {
            _shards = shards;
        }

        public List<Shard> GetAllShards()
        {
            List<Shard> outputShards = new List<Shard>();

            foreach (Shard shard in _shards)
                outputShards.Add(shard);

            return outputShards;
        }

        public Shard GetShardById(int shardId)
        {
            return _shards[shardId - 1];
        }
    }
}
