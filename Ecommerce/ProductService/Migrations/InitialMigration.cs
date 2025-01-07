using FluentMigrator;

namespace ProductService.Migrations
{
    [Migration(0, "Initial Migration")]
    public class InitialMigration : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"CREATE TABLE Products (Id SERIAL PRIMARY KEY, Name TEXT, Description TEXT, Price DECIMAL, Stock INT);");
            Execute.Sql(@"ALTER TABLE Products ADD UNIQUE (name);");
            Execute.Sql(@"CREATE INDEX index_products_name ON Products (name);");
        }

        public override void Down()
        {

        }
    }
}
