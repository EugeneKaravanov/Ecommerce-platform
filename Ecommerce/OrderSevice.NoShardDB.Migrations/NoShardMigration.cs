using FluentMigrator;

namespace OrderService.Migrations.NoShardDB
{
    [Migration(0, "Initial Migration For Orders")]
    public class NoShardMigration : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"CREATE TABLE Orders 
                        (
                            Id SERIAL PRIMARY KEY,
                            CustomerId INT,
                            OrderDate TIMESTAMP,
                            TotalAmount DECIMAL
                        );");
            Execute.Sql(@"CREATE INDEX IF NOT EXISTS index_customer_id ON Orders (CustomerId);");
            Execute.Sql(@"CREATE TABLE OrderItems 
                        (
                            Id SERIAL PRIMARY KEY,
                            OrderId INT REFERENCES orders(id) ON DELETE CASCADE,
                            ProductId INT,
                            Quantity INT,
                            UnitPrice DECIMAL
                        );");
        }

        public override void Down() { }
    }
}
