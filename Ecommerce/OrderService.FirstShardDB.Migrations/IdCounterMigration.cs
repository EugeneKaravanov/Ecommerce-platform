using FluentMigrator;

namespace OrderService.Migrations.FirstShardDB
{
    [Migration(1, "Migration adding Id Counter for Orders")]
    public class IdCounterMigration : Migration
    {
        public override void Up()
        {
            Execute.Sql($"CREATE TABLE Bucket1.IdCounter (Id SERIAL PRIMARY KEY);");
        }

        public override void Down() { }
    }
}
