using FluentMigrator;

namespace OrderService.Migrations.FirstShardDB
{
    [Migration(2, "Migration adding CustomerId global index")]
    public class CustomerIdGlobalIndexMigration : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"CREATE TABLE Bucket2.CustomerIdGlobalIndex 
                        (
                            Id INT PRIMARY KEY,
                            CustomerId INT
                        );");
            Execute.Sql(@"CREATE INDEX index_customer_id ON Bucket2.CustomerIdGlobalIndex (CustomerId);");
        }

        public override void Down() { }
    }
}
