namespace OrderService.Migrations
{
    public static class SqlMethods
    {
        public static List<string> GetSqlCommandsToCreateNewBucket(int bucketId)
        {
            List<string> SqlCommands = new List<string>();
            int idCounterModifier = 1000000;

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
            SqlCommands.Add($@"CREATE SEQUENCE Bucket{bucketId}.IdCounter
                                    INCREMENT BY 1
                                    START WITH {bucketId * idCounterModifier}
                                    MINVALUE {bucketId * idCounterModifier}
                                    MAXVALUE {bucketId * idCounterModifier + idCounterModifier - 1}
                                    CACHE 1;");

            return SqlCommands;
        }
    }
}
