using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WowheadScraper;

public class Quest
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int RequiredLevel { get; set; }
    public string Type { get; set; }
    public bool? IsBattleground { get; set; }
    public string Faction { get; set; }
    public bool IsRepeatable { get; set; }
    public bool? IsManuallyRepeatableOverride { get; set; }
    public int RequiredMoney { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public MoneyReward Money { get; set; }
    public Dictionary<int, int> Experience { get; set; }
    public List<ReputationReward> Reputations { get; set; }
    public string ErrorMessage { get; set; }

    public Quest()
    {
        Money = new MoneyReward();
        Experience = new Dictionary<int, int>();
        Reputations = new List<ReputationReward>();
    }
    
    public bool IsAvailable => string.IsNullOrWhiteSpace(ErrorMessage);
    
    public bool GetIsRepeatable()
    {
        if (IsManuallyRepeatableOverride.HasValue)
        {
            return IsManuallyRepeatableOverride.Value;
        }
        return IsRepeatable;
    }
    
    public static async Task<Quest> GetQuest(int id, IQuestSetup setup)
    {
        var html = await File.ReadAllTextAsync(setup.GetHtmlFilePath(id));

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        var questName = htmlDocument.DocumentNode.SelectSingleNode(".//h1[@class='heading-size-1']")?.InnerText;
        if (string.IsNullOrWhiteSpace(questName))
        {
            return new Quest {Id = id, ErrorMessage = "questName was empty"};
        }

        questName = WebUtility.HtmlDecode(questName);
        if (questName == setup.NotFoundName)
        {
            return new Quest {Id = id, ErrorMessage = "quest not found"};
        }

        var isException = false;
        var identifier = setup.NotAvailableNameIdentifiers.Find(identifier =>
            questName.Contains(identifier, StringComparison.InvariantCulture));
        if (identifier != null && !isException)
        {
            return new Quest {Id = id, Name = questName, ErrorMessage = $"questName has identifier {identifier}"};
        }


        var regexIdentifier = setup.NotAvailableNameRegexIdentifier.Find(regex => regex.IsMatch(questName));
        if (regexIdentifier != null && !isException)
        {
            return new Quest
            {
                Id = id, Name = questName,
                ErrorMessage = $"questName has identifier {regexIdentifier.ToString().Replace(".*", " ")}"
            };
        }

        if (setup.GetNotAvailableQuestIDs().TryGetValue(id, out var reason))
        {
            return new Quest {Id = id, Name = questName, ErrorMessage = reason};
        }

        var turnInMoney = 0;
        var turnInMoneyNode = htmlDocument.DocumentNode.SelectSingleNode(
            ".//table[@class='icon-list']/tr/td[contains(normalize-space(.), 'Required money:')]");
        if (turnInMoneyNode != null)
        {
            var gold = turnInMoneyNode.SelectSingleNode(".//span[@class='moneygold']")?.InnerText;
            var silver = turnInMoneyNode.SelectSingleNode(".//span[@class='moneysilver']")?.InnerText;
            var copper = turnInMoneyNode.SelectSingleNode(".//span[@class='moneycopper']")?.InnerText;

            turnInMoney = Program.GetMoney(gold, 10000) + Program.GetMoney(silver, 100) + Program.GetMoney(copper, 1);
        }

        var level = 0;
        var requiredLevel = 0;
        var isRepeatable = false;
        var questType = "";
        var faction = "";
        var quickFacts = htmlDocument.DocumentNode.SelectSingleNode(".//table[@class='infobox']")?
            .SelectSingleNode(".//tr/td/script[contains(normalize-space(.), 'WH.markup.printHtml(\"[ul][li]Level:')]");
        if (quickFacts != null)
        {
            var levelMatch = new Regex("Level: (\\d+)", RegexOptions.Compiled).Match(quickFacts.InnerText);
            if (levelMatch.Success)
            {
                level = int.Parse(levelMatch.Groups[1].Value);
            }

            var requiredLevelMatch = new Regex("Requires level (\\d+)", RegexOptions.Compiled).Match(quickFacts.InnerText);
            if (requiredLevelMatch.Success)
            {
                requiredLevel = int.Parse(requiredLevelMatch.Groups[1].Value);
            }
            
            var questTypeMatch = new Regex("Type: (\\w+)", RegexOptions.Compiled).Match(quickFacts.InnerText);
            if (questTypeMatch.Success)
            {
                questType = questTypeMatch.Groups[1].Value;
            }

            //Side: [span class=icon-alliance]Alliance
            var factionMatch = new Regex("Side: (?:\\[span class=.*?])?(\\w+)", RegexOptions.Compiled).Match(quickFacts.InnerText);
            if (factionMatch.Success)
            {
                faction = factionMatch.Groups[1].Value;
            }
            
            isRepeatable = quickFacts.InnerText.Contains("[li]Repeatable");
        }

        bool? isRepeatableOverride = null;
        if (QuestLists.ManualRepeatableOverride.TryGetValue(id, out var repeatableOverride))
        {
            isRepeatableOverride = repeatableOverride;
        }
        
        bool? isBattleground = null;
        if (QuestLists.BattlegroundQuests.Contains(id))
        {
            isBattleground = true;
        }

        var minLevel = 0;
        var maxLevel = 0;
        var money = new MoneyReward();
        var experience = new Dictionary<int, int>();
        var questRewardScript = htmlDocument.GetElementbyId("quest-reward-slider")?
            .NextSibling?
            .InnerHtml;
        if (questRewardScript != null)
        {
            var jsonQuestDetails = questRewardScript
                .Replace("WH.Wow.Quest.setupScalingRewards", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(";", "");

            try
            {
                var questDetails = JsonSerializer.Deserialize<QuestDetails>(jsonQuestDetails);
                if (questDetails != null)
                {
                    minLevel = questDetails.MinLevel;
                    maxLevel = questDetails.MaxLevel;

                    money = new MoneyReward
                    {
                        QuestReward = questDetails.Coin.Levels.ToDictionary(),
                        ExperienceToMoney = questDetails.Coin.RewardAtCap
                    };

                    experience = questDetails.Xp.Levels.ToDictionary();
                }
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        var reputations = new List<ReputationReward>();
        var reputationNodes =
            htmlDocument.DocumentNode.SelectNodes(".//ul/li/div[contains(normalize-space(.), 'reputation with')]");
        if (reputationNodes != null)
        {
            foreach (var reputationNode in reputationNodes)
            {
                var reputation = new ReputationReward();

                var repString = reputationNode.SelectSingleNode(".//span")?.InnerText;
                if (int.TryParse(repString, out var amount))
                {
                    reputation.Amount = amount;
                }

                var reputationLink = reputationNode.SelectSingleNode(".//a");
                if (reputationLink != null)
                {
                    if (!string.IsNullOrWhiteSpace(reputationLink.InnerText))
                    {
                        reputation.Name = reputationLink.InnerText;
                    }

                    var factionLink = reputationLink.GetAttributeValue("href", "");
                    var factionIdMatch = new Regex("faction=(\\d+)", RegexOptions.Compiled).Match(factionLink);
                    if (factionIdMatch.Success)
                    {
                        reputation.Id = int.Parse(factionIdMatch.Groups[1].Value);
                    }
                }

                reputations.Add(reputation);
            }
        }

        return new Quest
        {
            Id = id,
            Name = questName,
            Level = level,
            RequiredLevel = requiredLevel,
            Type = questType,
            IsBattleground = isBattleground,
            Faction = faction,
            IsRepeatable = isRepeatable,
            IsManuallyRepeatableOverride = isRepeatableOverride,
            RequiredMoney = turnInMoney,
            MinLevel = minLevel,
            MaxLevel = maxLevel,
            Experience = experience,
            Money = money,
            Reputations = reputations
        };
    }

    public static int GetXpForLevel(int fullXp, int playerLevel, int questLevel)
    {
        var levelDifference = playerLevel - questLevel; // quest and player level difference
        if (levelDifference <= 5)
        {
            return RoundXp(fullXp);
        }

        var xpModifier = 0.1m;
        if (levelDifference < 10)
        {
            xpModifier = 1 - (levelDifference - 5) * 0.2m; // reduction function in a single statement
        }

        var reducedXp = fullXp * xpModifier;
        return RoundXp(reducedXp);
    }

    public static int RoundXp(decimal xp)
    {
        var step = xp > 1000 ? 50m : 10m;
        var roundXp = Math.Round(xp / step, MidpointRounding.AwayFromZero) * step;
        return decimal.ToInt32(roundXp);
    }
}

public class ReputationReward
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Amount { get; set; }
}

public class MoneyReward
{
    public MoneyReward()
    {
        QuestReward = new Dictionary<int, int>();
    }

    public Dictionary<int, int> QuestReward { get; set; }
    public int ExperienceToMoney { get; set; }
}