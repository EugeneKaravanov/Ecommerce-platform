using FluentMigrator;

namespace OrderService.Migrations
{
    [Migration(0, "Initial Migration For Orders")]
    public class InitialMigrationForOrderItems : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"CREATE TABLE Orders (Id SERIAL PRIMARY KEY, CustomerId INT, OrderDate TIMESTAMP, TotalAmount DECIMAL);");
            Execute.Sql(@"CREATE INDEX index_customer_id ON Orders (CustomerId);");
        }

        public override void Down()
        {
            Execute.Sql("DROP INDEX index_customer_id;");
            Execute.Sql("DROP TABLE Orders;");
        }
    }
}
