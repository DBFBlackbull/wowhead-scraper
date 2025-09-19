using System.Collections.Concurrent;

namespace WowheadScraper;

public interface IQuestGetter
{
    public Task<Quest> GetQuest(int id);
}

public class HtmlQuestGetter : IQuestGetter
{
    public async Task<Quest> GetQuest(int id)
    {
        return await Quest.GetQuest(id);
    }
}

public class TaskQuestGetter : IQuestGetter
{
    private ConcurrentDictionary<int, TaskCompletionSource<Quest>> _tasks;
    
    public TaskQuestGetter(ConcurrentDictionary<int, TaskCompletionSource<Quest>> tasks)
    {
        _tasks = tasks;
    }
    
    public async Task<Quest> GetQuest(int id)
    {
        var tcs = _tasks[id];
        return await tcs.Task;
    }
}