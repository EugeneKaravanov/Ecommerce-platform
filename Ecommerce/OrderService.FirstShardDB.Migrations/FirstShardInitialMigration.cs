using FluentMigrator;
using OrderService.Models;
using OrderService.Utilities.Factories;

namespace OrderService.Migrations.FirstShardDB
{
    [Migration(0, "Initial Migration For First Shard")]
    public class FirstShardInitialMigration : Migration
    {
        private readonly ShardFactory _shardFactory;
        private readonly int _shardId = 1;
        private readonly Shard _shard;

        public FirstShardInitialMigration(ShardFactory shardFactory)
        {
            _shardFactory = shardFactory;
            _shard = _shardFactory.GetShardById(_shardId);
        }

        public override void Up()
        {
            for (int i = 0; i < _shard.Buckets.Length; i++)
            {
                List<string> SqlCommands = SqlMethods.GetSqlCommandsToCreateNewBucket(_shard.GetBucketId(i));

                for (int j = 0; j < SqlCommands.Count; j++)
                    Execute.Sql(SqlCommands[j]);
            }
        }

        public override void Down() { }
    }
}
