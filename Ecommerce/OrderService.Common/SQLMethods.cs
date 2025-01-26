using FluentMigrator;
using FluentMigrator.Expressions;

namespace OrderService.Migrations
{
    public static class SqlMethods
    {
        public static List<string> GetSqlCommandsToCreateNewBucket(int bucketId)
        {
            List<string> SqlCommands = new List<string>();

            SqlCommands.Add($@"CREATE SCHEMA Bucket{bucketId};");
            SqlCommands.Add($@"CREATE TABLE Bucket{bucketId}.Orders 
                            (
                                Id INT PRIMARY KEY,
                                CustomerId INT,
                                OrderDate TIMESTAMP,
                                TotalAmount DECIMAL
                            );");
            SqlCommands.Add($@"CREATE INDEX IF NOT EXISTS index_customer_id_{bucketId} 
                               ON Bucket{bucketId}.Orders (CustomerId);");
            SqlCommands.Add($@"CREATE TABLE Bucket{bucketId}.OrderItems 
                            (
                                Id SERIAL PRIMARY KEY,
                                OrderId INT REFERENCES Bucket{bucketId}.orders(id) ON DELETE CASCADE,
                                ProductId INT,
                                Quantity INT,
                                UnitPrice DECIMAL
                            );");

            return SqlCommands;
        }
    }
}
