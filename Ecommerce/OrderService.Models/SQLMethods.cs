using FluentMigrator;
using FluentMigrator.Expressions;

namespace OrderService.Migrations
{
    public static class SQLMethods
    {
        public static List<string> GetSQLCommandsToCreateNewBucket(int bucketId)
        {
            List<string> SQLCommands = new List<string>();

            SQLCommands.Add($"CREATE SCHEMA Bucket{bucketId};");
            SQLCommands.Add($"CREATE TABLE Bucket{bucketId}.Orders (Id SERIAL PRIMARY KEY, CustomerId INT, RegionId INT, OrderDate TIMESTAMP, TotalAmount DECIMAL);");
            SQLCommands.Add($"CREATE INDEX index_customer_id ON Bucket{bucketId}.Orders (CustomerId);");
            SQLCommands.Add($"CREATE TABLE Bucket{bucketId}.OrderItems (Id SERIAL PRIMARY KEY, OrderId INT REFERENCES Bucket{bucketId}.orders(id), ProductId INT, Quantity INT, UnitPrice DECIMAL);");

            return SQLCommands;
        }
    }
}
