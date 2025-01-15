using OrderService.Models;

namespace OrderService.Utilities.Factories
{
    public class ShardFactory
    {
        private List<Shard> _shards = new List<Shard>();

        public ShardFactory(List<string> connectionStrings) 
        {
            _shards.Add(new Shard(connectionStrings[0], new List<int> { 0, 1, 2, 3}));
            _shards.Add(new Shard(connectionStrings[1], new List<int> { 4, 5, 6, 7 }));
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
