namespace WowheadScraper.quest;

public class Quest
{
    public const int LastQuestIdInClassic = 10000;
    public static readonly string HtmlFolder = Path.Join(Program.SolutionDirectory(), "classic", "quests");
    public static readonly string AvailableItemsTsv = Path.Join(Program.TsvFolder, "availableQuests.tsv");
    public static readonly string NotAvailableItemsTsv = Path.Join(Program.TsvFolder, "notAvailableItems.tsv");

    public int Id { get; set; }
    public string Name { get; set; }
    public int SellPrice { get; set; }
    public string ErrorMessage { get; set; }
}