using System.Text.RegularExpressions;

namespace WowheadScraper;

public class SetupTBCQuests : SetupBaseQuests, IQuestSetup
{
    public override int LastId => 12515;
    public override int MaxLevel => 70;
    public override string ExpansionPath => "tbc";
    public override string NotFoundName => "TBC Quests";
    public List<string> NotAvailableNameIdentifiers => QuestLists.NotAvailableNameIdentifiers;
    public List<Regex> NotAvailableNameRegexIdentifier => QuestLists.NotAvailableNameRegexIdentifier;
    private readonly Dictionary<int, string> _notAvailableQuestIDs = QuestLists.NotAvailableQuestIDs
        .Concat(QuestLists.WotLkQuestsIDs)
        .ToDictionary(kv => kv.Key, kv => kv.Value);
    public Dictionary<int, string> GetNotAvailableQuestIDs()
    {
        return _notAvailableQuestIDs;
    }
}