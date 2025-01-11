using FluentMigrator.Runner;
using OrderService.Utilities.Factories;
using System.Reflection;

namespace OrderService.Migrations
{
    public class CustomMigrationRunner
    {
        private readonly List<string> _shardConnectionStrings;

        public CustomMigrationRunner(List<string> shardConnectionStrings)
        {
            _shardConnectionStrings = shardConnectionStrings;
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
                    .AddSingleton(_shardConnectionStrings)                    
                    .BuildServiceProvider(false);

            return serviceProvider;
        }
    }
}
