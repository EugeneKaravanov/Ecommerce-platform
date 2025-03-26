using StackExchange.Redis;

namespace ProductService.Utilities.Redis
{
    public static class RedisExtensions
    {
        public static async Task<bool> TransactAsync(this IDatabase db, Action<RedisCommandQueue> addCommands)
        {
            var transaction = db.CreateTransaction();
            var queue = new RedisCommandQueue(transaction);

            addCommands(queue);

            if (await transaction.ExecuteAsync())
            {
                await queue.CompleteAsync();

                return true;
            }

            return false;
        }
    }
}
