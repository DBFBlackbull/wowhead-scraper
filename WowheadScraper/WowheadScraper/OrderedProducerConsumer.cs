using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using HtmlAgilityPack;

namespace WowheadScraper;

public class OrderedProducerConsumer
{
    // The "promise board" where producers post their results.
    // The consumer looks here to await specific keys.
    private readonly ConcurrentDictionary<int, TaskCompletionSource<Item>> _tasks = new();

    // A thread-safe way to get a unique integer key.
    private int _currentItemId = 0;

    public async Task Run(int producerCount, int itemsToProcess)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Console.WriteLine($"Starting with {producerCount} producers to process {itemsToProcess} items IN ORDER.");

        // 1. Create all the "promise" tasks BEFORE starting the work.
        for (int i = 1; i <= itemsToProcess; i++)
        {
            _tasks[i] = new TaskCompletionSource<Item>();
        }

        // 2. Start the single consumer task
        var consumer = Task.Run(() => Consume(itemsToProcess));

        // 3. Start all the producer tasks
        var producers = new List<Task>();
        for (int i = 0; i < producerCount; i++)
        {
            producers.Add(Task.Run(() => Produce(itemsToProcess)));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(producers);
        await consumer;

        Console.WriteLine();
        Console.WriteLine($@"Processing complete. Elapsed Time: {stopwatch.Elapsed:T}");
    }

    /// <summary>
    /// The producer method. Its job is to get a key, do the work, 
    /// and then set the result on the corresponding TaskCompletionSource.
    /// </summary>
    private async Task Produce(int itemsToProcess)
    {
        while (true)
        {
            int key = Interlocked.Increment(ref _currentItemId);
            if (key > itemsToProcess)
            {
                break;
            }

            // --- Simulate slow work ---
            var item = await GetItem(key);
            // -------------------------

            // Find the "promise" for this key and set its result.
            // This unblocks the consumer if it's waiting for this specific key.
            if (_tasks.TryGetValue(key, out var tcs))
            {
                tcs.SetResult(item);
            }
        }
    }

    /// <summary>
    /// The consumer method. It now iterates sequentially and waits for each task to complete.
    /// </summary>
    private async Task Consume(int itemsToProcess)
    {
        var available = Path.Join(Program.SolutionDirectory(), "availableItems.txt");
        var notAvailable = Path.Join(Program.SolutionDirectory(), "notAvailableItems.txt");

        await using (var availableStream = new StreamWriter(File.Create(available)))
        {
            availableStream.AutoFlush = true;
            await using (var notAvailableStream = new StreamWriter(File.Create(notAvailable)))
            {
                notAvailableStream.AutoFlush = true;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int key = 1; key <= itemsToProcess; key++)
                {
                    // Get the promise for the key we need
                    var tcs = _tasks[key];

                    // Asynchronously and efficiently wait for the producer to call SetResult() on this specific task.
                    // If the producer is already done, this completes instantly.
                    Item item = await tcs.Task;

                    // --- Handle the data in strict order ---
                    if (item.IsAvailable)
                    {
                        await availableStream.WriteLineAsync($"[\"item:{key}\"] = {item.SellPrice},\t//{item.Name}");
                    }
                    else
                    {
                        await notAvailableStream.WriteLineAsync(item.ErrorMessage);
                    }

                    _tasks.TryRemove(key, out _);

                    if (key % 100 == 0)
                    {
                        Console.WriteLine(
                            $"[{DateTime.Now:t}] {key} / {itemsToProcess} completed. Elapsed {stopwatch.Elapsed.Seconds} seconds");
                        stopwatch.Restart();
                    }
                    // ------------------------------------
                }
            }
        }
    }

    private static async Task<Item> GetItem(int i)
    {
        string html;
        using (var response = await Program.HttpClient.GetAsync(new Uri($"classic/item={i}", UriKind.Relative)))
        {
            if (!response.IsSuccessStatusCode)
            {
                return new Item {ErrorMessage = $"{i}: failed to get item: {response.StatusCode}"};
            }

            using (var responseContent = response.Content)
            {
                html = await responseContent.ReadAsStringAsync();
            }
        }

        return Program.GetItem(i, html);
    }
}