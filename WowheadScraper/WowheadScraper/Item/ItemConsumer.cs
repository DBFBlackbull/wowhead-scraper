using System.Collections.Concurrent;
using System.Diagnostics;

namespace WowheadScraper;

public class ItemConsumer
{
    public static async Task Run(ITaskGetter<Item> itemGetter, IItemSetup setup)
    {
        Directory.CreateDirectory(Program.TsvFolderPath);
        
        var totalStopwatch = new Stopwatch();
        totalStopwatch.Start();
        Console.WriteLine($"Starting consuming {setup.LastId} items...");
        
        await using (var availableStream = new StreamWriter(File.Create(setup.AvailableTsvFilePath)))
        {
            availableStream.AutoFlush = true;
            await using (var notAvailableStream = new StreamWriter(File.Create(setup.NotAvailableTsvFilePath)))
            {
                notAvailableStream.AutoFlush = true;

                await availableStream.WriteLineAsync("id\tname\tsellPriceCopper");
                await notAvailableStream.WriteLineAsync("id\tname\treason");
                
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int id = 1; id <= setup.LastId; id++)
                {
                    var item = await itemGetter.GetTask(id);
                    if (item.IsAvailable)
                    {
                        await availableStream.WriteLineAsync($"{item.Id}\t{item.Name}\t{item.SellPrice}");
                    }
                    else
                    {
                        await notAvailableStream.WriteLineAsync($"{item.Id}\t{item.Name}\t{item.ErrorMessage}");
                    }

                    Program.LogProgress(id, setup.LastId, stopwatch);
                    // ------------------------------------
                }
            }
        }
        
        Program.LogJobDone("items", totalStopwatch);
    }
}