using System.Collections.Concurrent;

namespace WowheadScraper;

public interface IItemGetter
{
    public Task<Item> GetItem(int id);
}

public class HtmlItemGetter : IItemGetter
{
    public async Task<Item> GetItem(int id)
    {
        return await Item.GetItem(id);
    }
}

public class TaskItemGetter : IItemGetter
{
    private ConcurrentDictionary<int, TaskCompletionSource<Item>> _tasks;
    
    public TaskItemGetter(ConcurrentDictionary<int, TaskCompletionSource<Item>> tasks)
    {
        _tasks = tasks;
    }
    
    public async Task<Item> GetItem(int id)
    {
        var tcs = _tasks[id];
        return await tcs.Task;
    }
}