using FluentMigrator;

namespace OrderService.Migrations.FirstShardDB
{
    [Migration(3, "Migration adding RegionId global index")]
    public class RegionIdGlobalIndexMigration : Migration
    {
        public override void Up()
        {
            Execute.Sql($"CREATE TABLE Bucket3.CustomerIdGlobalIndex (Id INT PRIMARY KEY, RegionId INT);");
        }

        public override void Down() { }
    }
}
