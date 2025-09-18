using System.Diagnostics;
using System.Text;

namespace WowheadScraper;

public class HtmlProducer
{
    // Use a thread-safe counter to give each producer a unique ID for its work
    private int _currentItemId = 0;

    public async Task Run(int producerCount, int itemsToProcess, string prefix)
    {
        Directory.CreateDirectory(Item.HtmlFolderPath);
        
        Console.WriteLine($"Starting {producerCount} producers...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // 1. Create a list to hold all the tasks.
        var producerTasks = new List<Task>();

        // 2. Start all the producer tasks and add them to the list.
        for (int i = 0; i < producerCount; i++)
        {
            // Task.Run() starts the work on a background thread from the thread pool.
            producerTasks.Add(Task.Run(() => GetAndSaveHtml(prefix)));
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
    private async Task GetAndSaveHtml(string prefix)
    {
        while (true)
        {
            // Get a unique ID for this piece of work
            int key = Interlocked.Increment(ref _currentItemId);
            if (key > Item.LastItemIdInClassic)
            {
                break;
            }

            var html = await GetItemHtml(key, prefix);
            var filePath = Path.Join(Item.HtmlFolderPath, $"{prefix}-{key}.html");
            await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);
            
            Program.LogProgress(key, Item.LastItemIdInClassic);
        }
    }
    
    private static async Task<string> GetItemHtml(int i, string prefix)
    {
        using var response = await Program.HttpClient.GetAsync(new Uri($"classic/{prefix}={i}", UriKind.Relative));
        using var content = response.Content;
        return await content.ReadAsStringAsync();
    }
}