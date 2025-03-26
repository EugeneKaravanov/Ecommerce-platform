using StackExchange.Redis;

namespace ProductService.Utilities.Redis
{
    public class RedisCommandQueue
    {
        private readonly ITransaction _transaction;
        private readonly IList<Task> _tasks = new List<Task>();

        public RedisCommandQueue Enqueue(Func<ITransaction, Task> cmd)
        {
            _tasks.Add(cmd(_transaction));
            return this;
        }

        internal RedisCommandQueue(ITransaction transaction) => _transaction = transaction;
        internal Task CompleteAsync() => Task.WhenAll(_tasks);
    }
}
