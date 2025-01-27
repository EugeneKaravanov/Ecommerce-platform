using FluentMigrator.Runner;
using OrderService.Models;
using OrderService.Utilities.Factories;
using System.Reflection;

namespace OrderService.Migrations
{
    public class CustomMigrationRunner
    {
        private readonly List<Shard> _shards;

        public CustomMigrationRunner(List<Shard> shards)
        {
            _shards = shards;
        }

        public void RunMigrations(string connectionString, Assembly assembly)
        {
            var serviceProvider = ConfigureMigrations(connectionString, assembly);

            using (var scope = serviceProvider.CreateScope())
            {
                var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
                runner.MigrateUp();
            }
        }

        private ServiceProvider ConfigureMigrations(string connectionString, Assembly assembly)
        {
            var serviceProvider = new ServiceCollection().AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(assembly).For.Migrations())
                    .AddTransient<ShardFactory>()
                    .AddSingleton(_shards)                    
                    .BuildServiceProvider(false);

            return serviceProvider;
        }
    }
}
