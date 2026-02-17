using System.Diagnostics;
using System.Net;
using System.Text;

namespace WowheadScraper;

public class HtmlProducer
{
    // Use a thread-safe counter to give each producer a unique ID for its work
    private static int _currentId = 0;
    private static readonly string ServerTime = $"\"serverTime\":\"{DateTime.Today:yyyy-MM-dd}";

    private static ManualResetEventSlim _canProceed = new(true);
    private static bool _isHandlingBlock = false;
    private static readonly Lock RetryLock = new();

    public async Task Run(int producerCount, IPathsGetter pathsGetter)
    {
        Directory.CreateDirectory(pathsGetter.GetHtmlFolderPath());

        Console.WriteLine($"Starting {producerCount} html producers...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // 1. Create a list to hold all the tasks.
        var producerTasks = new List<Task>();

        // 2. Start all the producer tasks and add them to the list.
        for (int i = 0; i < producerCount; i++)
        {
            // Task.Run() starts the work on a background thread from the thread pool.
            producerTasks.Add(Task.Run(() => GetAndSaveHtml(pathsGetter)));
        }

        // 3. Wait for ALL tasks in the list to complete. 🏁
        await Task.WhenAll(producerTasks);

        Program.LogJobDone("producers", stopwatch);
    }

    /// <summary>
    /// This is the method each producer thread will run.
    /// It gets a unique piece of work, processes it, and saves it to disk.
    /// </summary>
    private async Task GetAndSaveHtml(IPathsGetter pathsGetter)
    {
        while (true)
        {
            // Get a unique ID for this piece of work
            int id = Interlocked.Increment(ref _currentId);
            if (id > pathsGetter.LastId)
            {
                break;
            }

            // Since Wowhead has started blocking IPs that request too many pages,
            // We now check the current file to see if it is up to date
            var filePath = pathsGetter.GetHtmlFilePath(id);
            if (!File.Exists(filePath))
            {
                await File.Create(filePath).DisposeAsync();
            }
            var oldHtml = await File.ReadAllTextAsync(filePath);
            if (oldHtml.Contains(ServerTime))
            {
                Program.LogProgress(id, pathsGetter.LastId);
                continue;
            }

            var requestUri = pathsGetter.GetUri(id);
            using var response = await GetResponse(requestUri);
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                Program.Log($"Forbidden reading id {requestUri.ToString()}. Stopping HTML producer.");
                return;
            }

            using var content = response.Content;
            var html = await content.ReadAsStringAsync();

            if (html.Contains("ERROR: The request could not be satisfied",
                    StringComparison.InvariantCultureIgnoreCase) ||
                html.Contains("504 Gateway Timeout ERROR", StringComparison.InvariantCultureIgnoreCase))
            {
                Program.Log($"Error reading id {requestUri.ToString()}");
            }

            await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);

            Program.LogProgress(id, pathsGetter.LastId);
        }
    }

    private static async Task<HttpResponseMessage> GetResponse(Uri requestUri)
    {
        var response = await Program.HttpClient.GetAsync(requestUri);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var shouldHandleBlock = false;
            
            lock (RetryLock)
            {
                if (!_isHandlingBlock)
                {
                    _isHandlingBlock = true;
                    shouldHandleBlock = true;
                    _canProceed.Reset(); // block all other threads
                    Program.Log("Wowhead is blocked. Retrying...");
                }
            }

            if (!shouldHandleBlock)
            {
                // Another thread is already handling the block, just wait for it to finish
                _canProceed.Wait();
                return await Program.HttpClient.GetAsync(requestUri);
            }

            // This thread is responsible for handling the block
            var retry = 0;
            var success = false;
            while (!success)
            {
                retry++;
                await Task.Delay(TimeSpan.FromMinutes(1));

                response = await Program.HttpClient.GetAsync(requestUri);
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Program.Log($"Wowhead is still blocked after {retry} minutes...");
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    success = true;
                    _isHandlingBlock = false;
                    _canProceed.Set(); // allow other threads to proceed
                    Program.Log("Wowhead is available again. Continuing.");
                }
            }
        }

        return response;
    }
}