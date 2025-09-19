using System.Net;
using System.Text.Json;
using HtmlAgilityPack;

namespace WowheadScraper;

public class Quest : IHtmlProducerPaths
{
    public const int LastIdInClassic = 9273; // TODO Find the real number
    public static readonly string HtmlFolderPath = Path.Join(Program.SolutionDirectory(), "classic", "quests");
    public static readonly string AvailableTsvFilePath = Path.Join(Program.TsvFolderPath, "quests-available.tsv");
    public static readonly string NotAvailableTsvFilePath = Path.Join(Program.TsvFolderPath, "quests-not-available.tsv");
    private static string HtmlFilePath(int id) => Path.Join(HtmlFolderPath, $"quest-{id}.html");
    public Uri GetUri(int id) => new Uri($"classic/quest={id}", UriKind.Relative);

    public string GetHtmlFolderPath() => HtmlFolderPath;
    public string GetHtmlFilePath(int id) => Path.Join(HtmlFolderPath, $"quest-{id}.html");
    
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsRepeatable { get; set; }
    public MoneyReward Money { get; set; }
    public ExperienceReward Experience { get; set; }
    public List<Reputation> Reputations { get; set; }
    public string ErrorMessage { get; set; }

    public Quest()
    {
        Reputations = new List<Reputation>();
    }
    
    public bool IsAvailable => string.IsNullOrWhiteSpace(ErrorMessage);
    
    public static async Task<Quest> GetQuest(int id)
    {
        var html = await File.ReadAllTextAsync(HtmlFilePath(id));
        
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        var questName = htmlDocument.DocumentNode.SelectSingleNode(".//h1[@class='heading-size-1']")?.InnerText;
        if (string.IsNullOrWhiteSpace(questName))
        {
            return new Quest {Id = id, ErrorMessage = "questName was empty"};
        }

        questName = WebUtility.HtmlDecode(questName);
        if (questName == "Classic Quests")
        {
            return new Quest {Id = id, ErrorMessage = "quest not found"};
        }

        var isException = false; //NotAvailableExceptions.Contains(id);
        // var identifier = Program.NotAvailableNameIdentifiers.Find(identifier =>
        //     questName.Contains(identifier, StringComparison.InvariantCulture));
        // if (identifier != null && !isException)
        // {
        //     return new Quest {Id = id, Name = questName, ErrorMessage = $"questName has identifier {identifier}"};
        // }

        
        var quickFacts = htmlDocument.DocumentNode.SelectSingleNode(".//table[@class='infobox after-buttons']")?.InnerHtml;
        if (quickFacts != null)
        {
            var regex = Program.NotAvailableQuickFactsIdentifier.Find(regex =>
                regex.IsMatch(quickFacts));
            if (regex != null && !isException)
            {
                return new Quest {Id = id, Name = questName, ErrorMessage = $"quest quick facts has identifier {regex.Replace(".*", " ")}"};
            }
        }
        
        // if (html.Contains("This item is not available to players.") && !isException)
        // {
        //     return new Quest {Id = id, Name = questName, ErrorMessage = "item is not available to players"};
        // }

        var money = new MoneyReward();
        var experience = new ExperienceReward();
        var questDetailsScript = htmlDocument.DocumentNode.SelectSingleNode(".//div[@class='quest-reward-slider']")
            ?.NextSibling?.InnerHtml;
        if (questDetailsScript != null)
        {
            var jsonQuestDetails = questDetailsScript
                .Replace("WH.Wow.Quest.setupScalingRewards", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(";", "");

            var questDetails  = JsonSerializer.Deserialize<QuestDetails>(jsonQuestDetails);
            if (questDetails != null)
            {
                money = new MoneyReward
                {
                    QuestReward = questDetails.Coin.GetLevelsReward(),
                    ExperienceToMoney = questDetails.Coin.RewardAtCap
                };

                experience = new ExperienceReward
                {
                    Level = questDetails.Xp.Levels.Keys.FirstOrDefault(),
                    Experience = questDetails.Xp.Levels.Values.FirstOrDefault()
                };
            }
        }

        return new Quest {Id = id, Name = questName, Experience = experience, Money = money};
    }

}

public class Reputation
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Amount { get; set; }
}

public class MoneyReward
{
    public int QuestReward { get; set; }
    public int ExperienceToMoney { get; set; }
    public int Total => QuestReward + ExperienceToMoney;
}

public class ExperienceReward
{
    public int Level { get; set; }
    public int Experience { get; set; }
}