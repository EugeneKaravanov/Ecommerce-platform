using FluentMigrator;

namespace OrderService.Migrations
{
    [Migration(1, "Initial Migration For OrderItems")]
    public class InitialMigrationForOrders : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"CREATE TABLE OrderItems (Id SERIAL PRIMARY KEY, OrderId INT REFERENCES orders(id), ProductId INT, Quantity INT, UnitPrice DECIMAL);");
        }

        public override void Down()
        {
            Execute.Sql("DROP TABLE OrderItems;");
        }
    }
}
