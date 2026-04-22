using System.Threading.Channels;

namespace barberShop
{
    public interface IBackgroundTaskQueue
    {
        ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, Task> workItem);
        ValueTask<Func<CancellationToken,Task>> DequeueAsync(CancellationToken cancellationToken);
    }
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Func<CancellationToken, Task>> _queue;

        public BackgroundTaskQueue(int capacity = 200)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<Func<CancellationToken,Task>>(options);
        }

        public ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken,Task> workItem) 
            => _queue.Writer.WriteAsync(workItem);

        public ValueTask<Func<CancellationToken,Task>> DequeueAsync(CancellationToken cancellationToken)
            => _queue.Reader.ReadAsync(cancellationToken);
    }

    
}
