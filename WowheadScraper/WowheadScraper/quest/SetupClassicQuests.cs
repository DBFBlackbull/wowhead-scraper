using System.Text.RegularExpressions;

namespace WowheadScraper;

public class SetupClassicQuests : SetupBaseQuests, IQuestSetup
{
    public override int LastId => 9665;
    public override int MaxLevel => 60;
    public override string ExpansionPath => "classic";
    public override string NotFoundName => "Classic Quests";
    
    private readonly Dictionary<int, string> _notAvailableQuestIDs = QuestLists.NotAvailableQuestIDs
        .Concat(QuestLists.TbcQuestsIDs)
        .Concat(QuestLists.WotLkQuestsIDs)
        .ToDictionary(kv => kv.Key, kv => kv.Value);
    public override Dictionary<int, string> GetNotAvailableQuestIDs()
    {
        return _notAvailableQuestIDs;
    }
}