using System.Diagnostics;
using System.Text;

namespace WowheadScraper;

public class HtmlProducer
{
    // Use a thread-safe counter to give each producer a unique ID for its work
    private int _currentId = 0;

    public async Task Run(int producerCount, int itemsToProcess, IHtmlProducerPaths paths)
    {
        Directory.CreateDirectory(paths.GetHtmlFolderPath());
        
        Console.WriteLine($"Starting {producerCount} html producers...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // 1. Create a list to hold all the tasks.
        var producerTasks = new List<Task>();

        // 2. Start all the producer tasks and add them to the list.
        for (int i = 0; i < producerCount; i++)
        {
            // Task.Run() starts the work on a background thread from the thread pool.
            producerTasks.Add(Task.Run(() => GetAndSaveHtml(itemsToProcess, paths)));
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
    private async Task GetAndSaveHtml(int itemsToProcess, IHtmlProducerPaths paths)
    {
        while (true)
        {
            // Get a unique ID for this piece of work
            int id = Interlocked.Increment(ref _currentId);
            if (id > itemsToProcess)
            {
                break;
            }

            using var response = await Program.HttpClient.GetAsync(paths.GetUri(id));
            using var content = response.Content;
            var html = await content.ReadAsStringAsync();
            
            if (html.Contains("ERROR: The request could not be satisfied", StringComparison.InvariantCultureIgnoreCase) || 
                html.Contains("504 Gateway Timeout ERROR", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine($"Error reading id {paths.GetUri(id).ToString()}");
            }
            
            var filePath = Path.Join(paths.GetHtmlFilePath(id));
            await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);
            
            Program.LogProgress(id, itemsToProcess);
        }
    }
}