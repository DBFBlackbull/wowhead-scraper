namespace WowheadScraper;

public class Quest
{
    public const int LastQuestIdInClassic = 9273; // TODO Find the real number
    public static readonly string HtmlFolderPath = Path.Join(Program.SolutionDirectory(), "classic", "quests");
    public static readonly string AvailableTsvFilePath = Path.Join(Program.TsvFolder, "quests-available.tsv");
    public static readonly string NotAvailableTsvFilePath = Path.Join(Program.TsvFolder, "quests-not-availableItems.tsv");

    public int Id { get; set; }
    public string Name { get; set; }
    public int Experience { get; set; }
    public List<Reputation> Reputations { get; set; }
}

public class Reputation
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Amount { get; set; }
}  