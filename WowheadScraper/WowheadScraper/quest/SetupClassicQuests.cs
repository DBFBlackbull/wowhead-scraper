using System.Text.RegularExpressions;

namespace WowheadScraper;

public class SetupClassicQuests : SetupBaseQuests, IQuestSetup
{
    public override int LastId => 9665;
    public override string ExpansionPath => "classic";
    public override string NotFoundName => "Classic Quest";
    public List<string> NotAvailableNameIdentifiers => QuestLists.NotAvailableNameIdentifiers;
    public List<Regex> NotAvailableNameRegexIdentifier => QuestLists.NotAvailableNameRegexIdentifier;
    private readonly Dictionary<int, string> _notAvailableQuestIDs = QuestLists.NotAvailableQuestIDs
        .Concat(QuestLists.TbcQuestsIDs)
        .Concat(QuestLists.WotLkQuestsIDs)
        .ToDictionary(kv => kv.Key, kv => kv.Value);
    public Dictionary<int, string> GetNotAvailableQuestIDs()
    {
        return _notAvailableQuestIDs;
    }
}