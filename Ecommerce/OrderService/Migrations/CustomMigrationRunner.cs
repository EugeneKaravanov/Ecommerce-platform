using FluentMigrator.Runner;
using OrderService.Migrations.NoShardDB;
using OrderService.Migrations.FirstShardDB;
using OrderService.Migrations.SecondShardDB;
using OrderService.Utilities.Factories;

namespace OrderService.Migrations
{
    public class CustomMigrationRunner
    {
        private readonly List<string> _shardConnectionStrings;

        public CustomMigrationRunner(List<string> shardConnectionStrings)
        {
            _shardConnectionStrings = shardConnectionStrings;
        }

        public void RunMigrationsForNoShardDB(string connectionString)
        {
            var serviceProvider = new ServiceCollection().AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(NoShardMigration).Assembly).For.Migrations())
                    .BuildServiceProvider(false);
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

            runner.MigrateUp();
        }

        public void RunMigrationsForFirstShardDB(string connectionString)
        {
            var serviceProvider = new ServiceCollection().AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(FirstShardInitialMigration).Assembly).For.Migrations())
                    .AddTransient<ShardFactory>()
                    .AddSingleton(_shardConnectionStrings)
                    .BuildServiceProvider(false);
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

            runner.MigrateUp();
        }

        public void RunMigrationForSecondShardDB(string connectionString)
        {
            var serviceProvider = new ServiceCollection().AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(SecondShardInitialMigration).Assembly).For.Migrations())
                    .AddTransient<ShardFactory>()
                    .AddSingleton(_shardConnectionStrings)
                    .BuildServiceProvider(false);
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

            runner.MigrateUp();
        }
    }
}
