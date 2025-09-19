using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using HtmlAgilityPack;

namespace WowheadScraper;

public class OrderedQuestProducerConsumer
{
    // The "promise board" where producers post their results.
    // The consumer looks here to await specific keys.
    private readonly ConcurrentDictionary<int, TaskCompletionSource<Quest>> _tasks = new();
 
    public async Task Run(int producerCount, int itemsToProcess = Quest.LastIdInClassic)
    {
        Console.WriteLine($"Starting with {producerCount} producers to process {itemsToProcess} items IN ORDER.");
        Console.WriteLine();

        // 1. Create all the "promise" tasks BEFORE starting the work.
        for (int i = 1; i <= itemsToProcess; i++)
        {
            _tasks[i] = new TaskCompletionSource<Quest>();
        }

        // 2. Start the single consumer task
        var consumer = Task.Run(() => QuestConsumer.Run(new TaskQuestGetter(_tasks), itemsToProcess));

        // 3. Start all the producer tasks
        var idGenerator = new IdGenerator();

        var producers = new List<Task>();
        for (int i = 0; i < producerCount; i++)
        {
            producers.Add(Task.Run(() => QuestProducer.Run(idGenerator, _tasks, itemsToProcess)));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(producers);
        await consumer;
    }
}