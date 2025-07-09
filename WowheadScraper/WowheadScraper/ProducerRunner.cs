using System.Diagnostics;
using System.Text;

namespace WowheadScraper;

public class ProducerRunner
{
    // Use a thread-safe counter to give each producer a unique ID for its work
    private int _currentItemId = 0;

    public async Task Run(int producerCount, int itemsToProcess)
    {
        Console.WriteLine($"Starting {producerCount} producers...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // 1. Create a list to hold all the tasks.
        var producerTasks = new List<Task>();

        // 2. Start all the producer tasks and add them to the list.
        for (int i = 0; i < producerCount; i++)
        {
            // Task.Run() starts the work on a background thread from the thread pool.
            producerTasks.Add(Task.Run(() => ProduceAndSaveWork(itemsToProcess)));
        }

        // 3. Wait for ALL tasks in the list to complete. 🏁
        await Task.WhenAll(producerTasks);

        Console.WriteLine();
        Console.WriteLine($"All producers have finished their work. Elapsed {stopwatch.Elapsed:g}");
    }

    /// <summary>
    /// This is the method each producer thread will run.
    /// It gets a unique piece of work, processes it, and saves it to disk.
    /// </summary>
    private async Task ProduceAndSaveWork(int itemsToProcess)
    {
        while (true)
        {
            // Get a unique ID for this piece of work
            int key = Interlocked.Increment(ref _currentItemId);
            if (key > itemsToProcess)
            {
                break;
            }

            if (key % 100 == 0)
            {
                Console.WriteLine($"[{DateTime.Now:t}] {key} / {itemsToProcess} completed.");
            }
            
            var html = await GetResponse(key);
            var filePath = Path.Join(Program.SolutionDirectory(), "classic", $"item-{key}.html");
            await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);
        }
    }
    
    public static async Task<string> GetResponse(int i)
    {
        using (var response = await Program.HttpClient.GetAsync(new Uri($"classic/item={i}", UriKind.Relative)))
        {
            using (var content = response.Content)
            {
                return await content.ReadAsStringAsync();
            }
        }
    }
}