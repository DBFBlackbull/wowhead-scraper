using System.Collections.Concurrent;

namespace WowheadScraper;

public class ItemProducer
{
    /// <summary>
    /// The producer method. Its job is to get a key, do the work, 
    /// and then set the result on the corresponding TaskCompletionSource.
    /// </summary>
    public static async Task Run(IdGenerator idGenerator, ConcurrentDictionary<int, TaskCompletionSource<Item>> tasks)
    {
        while (true)
        {
            var id = idGenerator.GetNextId();
            if (id > Item.LastItemIdInClassic)
            {
                break;
            }

            var filePath = Path.Join(Item.HtmlFolder, $"item-{id}.html");
            var html = await File.ReadAllTextAsync(filePath);
            var item = Item.GetItem(id, html);

            // Find the "promise" for this key and set its result.
            // This unblocks the consumer if it's waiting for this specific key.
            if (tasks.TryGetValue(id, out var tcs))
            {
                tcs.SetResult(item);
            }
        }
    }
}