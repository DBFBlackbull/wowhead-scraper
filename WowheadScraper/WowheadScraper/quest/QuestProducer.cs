using System.Collections.Concurrent;

namespace WowheadScraper;

public class QuestProducer
{
    /// <summary>
    /// The producer method. Its job is to get a key, do the work, 
    /// and then set the result on the corresponding TaskCompletionSource.
    /// </summary>
    public static async Task Run(IdGenerator idGenerator, ConcurrentDictionary<int, TaskCompletionSource<Quest>> tasks, int itemsToProcess = Quest.LastIdInClassic)
    {
        var questGetter = new HtmlQuestGetter();
        while (true)
        {
            var id = idGenerator.GetNextId();
            if (id > itemsToProcess)
            {
                break;
            }

            var quest = await questGetter.GetQuest(id);

            // Find the "promise" for this key and set its result.
            // This unblocks the consumer if it's waiting for this specific key.
            if (tasks.TryGetValue(id, out var tcs))
            {
                tcs.SetResult(quest);
            }
        }
    }
}