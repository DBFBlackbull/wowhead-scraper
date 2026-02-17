using System.Text.RegularExpressions;

namespace WowheadScraper;

public interface IQuestSetup : IPathsGetter
{
    public int MaxLevel { get; }
    public string NotFoundName { get; }
    public List<string> NotAvailableNameIdentifiers { get; }
    public List<Regex> NotAvailableNameRegexIdentifier { get; }
    public Dictionary<int, string> GetNotAvailableQuestIDs();
}