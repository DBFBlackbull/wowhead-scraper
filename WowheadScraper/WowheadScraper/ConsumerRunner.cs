using System.Diagnostics;

namespace WowheadScraper;

public class ConsumerRunner
{
    public async Task Run(int itemsToProcess)
    {
        var totalStopwatch = new Stopwatch();
        totalStopwatch.Start();
        Console.WriteLine($"Starting consuming {itemsToProcess} items...");
        
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
                    var filePath = Path.Join(Program.SolutionDirectory(), "classic", $"item-{key}.html");
                    var html = await File.ReadAllTextAsync(filePath);

                    var item = Program.GetItem(key, html);
                    if (item.IsAvailable)
                    {
                        await availableStream.WriteLineAsync($"[\"item:{key}\"] = {item.SellPrice},\t//{item.Name}");
                    }
                    else
                    {
                        await notAvailableStream.WriteLineAsync(item.ErrorMessage);
                    }

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
        
        Console.WriteLine();
        Console.WriteLine($"All items consumed. Elapsed {totalStopwatch.Elapsed:g}");
    }
}