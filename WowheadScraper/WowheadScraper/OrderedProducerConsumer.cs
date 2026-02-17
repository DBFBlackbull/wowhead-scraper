using System.Collections.Concurrent;

namespace WowheadScraper;

public class OrderedProducerConsumer<T>
{
    // The "promise board" where producers post their results.
    // The consumer looks here to await specific keys.
    private readonly ConcurrentDictionary<int, TaskCompletionSource<T>> _tasks = new();

    public async Task Run(
        int producerCount, 
        IPathsGetter pathsGetter, 
        Func<IdGenerator, ConcurrentDictionary<int, TaskCompletionSource<T>>, Task> producerFactory,
        Func<ITaskGetter<T>, Task> consumerFactory)
    {
        Directory.CreateDirectory(pathsGetter.TsvFolderPath);
        
        Console.WriteLine($"Starting with {producerCount} producers to process {pathsGetter.LastId} items IN ORDER.");
        Console.WriteLine();

        // 1. Create all the "promise" tasks BEFORE starting the work.
        for (int i = 1; i <= pathsGetter.LastId; i++)
        {
            _tasks[i] = new TaskCompletionSource<T>();
        }

        // 2. Start the single consumer task
        var consumer = Task.Run(() => consumerFactory(new TaskGetter<T>(_tasks)));

        // 3. Start all the producer tasks
        var idGenerator = new IdGenerator();

        var producers = new List<Task>();
        for (int i = 0; i < producerCount; i++)
        {
            producers.Add(Task.Run(() => producerFactory(idGenerator, _tasks)));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(producers);
        await consumer;
    }
}

public interface ITaskGetter<T>
{
    public Task<T> GetTask(int id);
}

public class TaskGetter<T> : ITaskGetter<T>
{
    private ConcurrentDictionary<int, TaskCompletionSource<T>> _tasks;
    
    public TaskGetter(ConcurrentDictionary<int, TaskCompletionSource<T>> tasks)
    {
        _tasks = tasks;
    }
    
    public async Task<T> GetTask(int id)
    {
        var tcs = _tasks[id];
        return await tcs.Task;
    }
}
