using System.Collections.Concurrent;
using System.Diagnostics;

namespace WowheadScraper;

public class ItemConsumer
{
    public static async Task Run(IItemGetter itemGetter, int itemsToProcess = Item.LastIdInClassic)
    {
        Directory.CreateDirectory(Program.TsvFolderPath);
        
        var totalStopwatch = new Stopwatch();
        totalStopwatch.Start();
        Console.WriteLine($"Starting consuming {itemsToProcess} items...");
        
        await using (var availableStream = new StreamWriter(File.Create(Item.AvailableTsvFilePath)))
        {
            availableStream.AutoFlush = true;
            await using (var notAvailableStream = new StreamWriter(File.Create(Item.NotAvailableTsvFilePath)))
            {
                notAvailableStream.AutoFlush = true;

                await availableStream.WriteLineAsync("id\tname\tsellPriceCopper");
                await notAvailableStream.WriteLineAsync("id\tname\treason");
                
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int id = 1; id <= itemsToProcess; id++)
                {
                    var item = await itemGetter.GetItem(id);
                    if (item.IsAvailable)
                    {
                        await availableStream.WriteLineAsync($"{item.Id}\t{item.Name}\t{item.SellPrice}");
                    }
                    else
                    {
                        await notAvailableStream.WriteLineAsync($"{item.Id}\t{item.Name}\t{item.ErrorMessage}");
                    }

                    Program.LogProgress(id, itemsToProcess, stopwatch);
                    // ------------------------------------
                }
            }
        }
        
        Console.WriteLine();
        Console.WriteLine($"All items consumed. Elapsed {totalStopwatch.Elapsed:g}");
    }
}