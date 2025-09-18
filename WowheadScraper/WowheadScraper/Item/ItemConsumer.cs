using System.Collections.Concurrent;
using System.Diagnostics;

namespace WowheadScraper;

public class ItemConsumer
{
    public static async Task Run(ConcurrentDictionary<int, TaskCompletionSource<Item>> tasks)
    {
        Directory.CreateDirectory(Program.TsvFolder);
        var available = Path.Join(Program.TsvFolder, "availableItems.tsv");
        var notAvailable = Path.Join(Program.TsvFolder, "notAvailableItems.tsv");
        
        var totalStopwatch = new Stopwatch();
        totalStopwatch.Start();
        Console.WriteLine($"Starting consuming {Item.LastItemIdInClassic} items...");
        
        await using (var availableStream = new StreamWriter(File.Create(available)))
        {
            availableStream.AutoFlush = true;
            await using (var notAvailableStream = new StreamWriter(File.Create(notAvailable)))
            {
                notAvailableStream.AutoFlush = true;

                await availableStream.WriteLineAsync("id\tname\tsellPriceCopper");
                await notAvailableStream.WriteLineAsync("id\tname\treason");
                
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int id = 1; id <= Item.LastItemIdInClassic; id++)
                {
                    // Get the promise for the key we need
                    var tcs = tasks[id];
                    
                    // If the producer is already done, this completes instantly.
                    Item item = await tcs.Task;
                    if (item.IsAvailable)
                    {
                        await availableStream.WriteLineAsync($"{item.Id}\t{item.Name}\t{item.SellPrice}");
                    }
                    else
                    {
                        await notAvailableStream.WriteLineAsync($"{item.Id}\t{item.Name}\t{item.ErrorMessage}");
                    }

                    Program.LogProgress(id, Item.LastItemIdInClassic, stopwatch);
                    // ------------------------------------
                }
            }
        }
        
        Console.WriteLine();
        Console.WriteLine($"All items consumed. Elapsed {totalStopwatch.Elapsed:g}");
    }

}