using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace WowheadScraper;

public abstract class SetupBaseQuests : IQuestSetup
{
    public abstract int LastId { get; }
    public abstract int MaxLevel { get; }
    public abstract string ExpansionPath { get; }
    public abstract string NotFoundName { get; }

    public List<string> NotAvailableNameIdentifiers => QuestLists.NotAvailableNameIdentifiers;
    public List<Regex> NotAvailableNameRegexIdentifier => QuestLists.NotAvailableNameRegexIdentifier;
    public abstract Dictionary<int, string> GetNotAvailableQuestIDs();

    public string TsvFolderPath => Path.Join(Program.TsvFolderPath, ExpansionPath);
    public string AvailableTsvFilePath => Path.Join(TsvFolderPath, "quests-available.tsv");
    public string NotAvailableTsvFilePath => Path.Join(TsvFolderPath, "quests-not-available.tsv");

    public Uri GetUri(int id) => new Uri($"{ExpansionPath}/quest={id}", UriKind.Relative);

    public string HtmlFolderPath => Path.Join(Program.SolutionDirectory(), ExpansionPath, "quests");
    public string GetHtmlFilePath(int id) => Path.Join(HtmlFolderPath, $"quest-{id}.html");
    
    public Func<IdGenerator, ConcurrentDictionary<int, TaskCompletionSource<Quest>>, Task> Producer()
    {
        return (idGenerator, tasks) => QuestProducer.Run(idGenerator, tasks, this);
    }
    
    public Func<ITaskGetter<Quest>, Task> Consumer()
    {
        return taskGetter => QuestConsumer.Run(taskGetter, this);
    }
}