using FluentMigrator;

namespace ProductService.Migrations
{
    [Migration(1, "Initial Migration")]
    public class IndexMigration : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"CREATE INDEX index_products_name ON Products (name);");
        }

        public override void Down()
        {
            Execute.Sql("DROP INDEX index_products_name;");
        }
    }
}
