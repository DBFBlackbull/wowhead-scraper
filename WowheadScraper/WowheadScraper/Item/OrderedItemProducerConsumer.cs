using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using HtmlAgilityPack;

namespace WowheadScraper;

public class OrderedItemProducerConsumer
{
    // The "promise board" where producers post their results.
    // The consumer looks here to await specific keys.
    private readonly ConcurrentDictionary<int, TaskCompletionSource<Item>> _tasks = new();

    public async Task Run(int producerCount)
    {
        Console.WriteLine($"Starting with {producerCount} producers to process {Item.LastItemIdInClassic} items IN ORDER.");
        Console.WriteLine();

        // 1. Create all the "promise" tasks BEFORE starting the work.
        for (int i = 1; i <= Item.LastItemIdInClassic; i++)
        {
            _tasks[i] = new TaskCompletionSource<Item>();
        }

        // 2. Start the single consumer task
        var consumer = Task.Run(() => ItemConsumer.Run(_tasks));

        // 3. Start all the producer tasks
        var idGenerator = new IdGenerator();

        var producers = new List<Task>();
        for (int i = 0; i < producerCount; i++)
        {
            producers.Add(Task.Run(() => ItemProducer.Run(idGenerator, _tasks)));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(producers);
        await consumer;
    }
}