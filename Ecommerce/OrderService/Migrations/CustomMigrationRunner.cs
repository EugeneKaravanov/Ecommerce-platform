using FluentMigrator.Runner;
using System.Reflection;

namespace OrderService.Migrations
{
    public class CustomMigrationRunner
    {
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
                    .BuildServiceProvider(false);

            return serviceProvider;
        }
    }
}
