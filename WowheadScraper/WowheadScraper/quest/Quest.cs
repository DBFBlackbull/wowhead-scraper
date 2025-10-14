using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WowheadScraper;

public class Quest : IHtmlProducerPaths
{
    public const int LastIdInClassic = 9665;
    public static readonly string HtmlFolderPath = Path.Join(Program.SolutionDirectory(), "classic", "quests");
    public static readonly string AvailableTsvFilePath = Path.Join(Program.TsvFolderPath, "quests-available.tsv");
    public static readonly string NotAvailableTsvFilePath = Path.Join(Program.TsvFolderPath, "quests-not-available.tsv");
    private static string HtmlFilePath(int id) => Path.Join(HtmlFolderPath, $"quest-{id}.html");
    public Uri GetUri(int id) => new Uri($"classic/quest={id}", UriKind.Relative);

    public string GetHtmlFolderPath() => HtmlFolderPath;
    public string GetHtmlFilePath(int id) => Path.Join(HtmlFolderPath, $"quest-{id}.html");
    
    public int Id { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int RequiredLevel { get; set; }
    public bool IsRepeatable { get; set; }
    public bool IsManuallyTaggedRepeatable => ManuallyTaggedRepeatable.Contains(Id);
    public int MoneyTurnIn { get; set; }
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
    
    private static readonly List<string> NotAvailableNameIdentifiers = new List<string>()
    {
        "(123)aa",
        "<CHANGE INTO GOSSIP>",
        "<CHANGE TO GOSSIP>",
        "[DEPRECATED]",
        "<nyi>",
        "<NYI>",
        "reuse",
        "REUSE",
        "<TEST>",
        "<TXT>",
        "<UNUSED>",
    };

    private static readonly Dictionary<int, string> NotAvailableQuests = new Dictionary<int, string>()
    {
        {3911, "duplicate quest"}, // The Last Element
        //{8856, "duplicate quest"}, // Desert Survival Kits 
        {7906, "test quest"}, // Darkmoon Cards - Beasts
        {7961, "test quest"}, // Waskily Wabbits!
        {7962, "test quest"}, // Wabbit Pelts
        {8530, "test quest"}, // The Alliance Needs Singed Corestones!
        {8531, "test quest"}, // The Alliance Needs More Singed Corestones!
        {8617, "test quest"}, // The Horde Needs Singed Corestones!
        {8618, "test quest"}, // The Horde Needs More Singed Corestones!
        {8325, "TBC quest"}, // Reclaiming Sunstrider Isle
        {8326, "TBC quest"}, // Unfortunate Measures
        {8327, "TBC quest"}, // Report to Lanthan Perilon
        {8328, "TBC quest"}, // Mage Training
        {8329, "TBC test quest"}, // Warrior Training
        {8563, "TBC quest"}, // Warlock Training
        {8564, "TBC quest"}, // Priest Training
        {9393, "TBC quest"}, // Hunter Training
        {9676, "TBC quest"}, // Paladin Training
        {8334, "TBC quest"}, // Aggression
        {8335, "TBC quest"}, // Felendren the Banished
        {8338, "TBC quest"}, // Tainted Arcane Sliver
        {8344, "TBC quest"}, // Windows to the Source
        {8347, "TBC quest"}, // Aiding the Outrunners
        {8350, "TBC quest"}, // Completing the Delivery
        {8463, "TBC quest"}, // Unstable Mana Crystals
        {8468, "TBC quest"}, // Wanted: Thaelis the Hungerer
        {8472, "TBC quest"}, // Major Malfunction
        {8473, "TBC quest"}, // A Somber Task
        {8474, "TBC quest"}, // Old Whitebark's Pendant
        {8475, "TBC quest"}, // The Dead Scar
        {8476, "TBC quest"}, // Amani Encroachment
        {8477, "TBC quest"}, // The Spearcrafter's Hammer
        {8478, "TBC test quest"}, // Choose Your Weapon
        {8479, "TBC quest"}, // Zul'Marosh
        {8480, "TBC quest"}, // Lost Armaments
        {8482, "TBC quest"}, // Incriminating Documents
        {8483, "TBC quest"}, // The Dwarven Spy
        {8486, "TBC quest"}, // Arcane Instability
        {8487, "TBC quest"}, // Corrupted Soil
        {8488, "TBC quest"}, // Unexpected Results
        {8489, "TBC test quest"}, // An Intact Converter
        {8490, "TBC quest"}, // Powering our Defenses
        {8491, "TBC quest"}, // Pelt Collection
        {8884, "TBC quest"}, // Fish Heads, Fish Heads...
        {8885, "TBC quest"}, // The Ring of Mmmrrrggglll
        {8886, "TBC quest"}, // Grimscale Pirates!
        {8887, "TBC quest"}, // Captain Kelisendra's Lost Rutters
        {8888, "TBC quest"}, // The Magister's Apprentice
        {8889, "TBC quest"}, // Deactivating the Spire
        {8890, "TBC quest"}, // Word from the Spire
        {8891, "TBC quest"}, // Abandoned Investigations
        {8892, "TBC quest"}, // Situation at Sunsail Anchorage
        {8894, "TBC quest"}, // Cleaning up the Grounds
        {8895, "TBC quest"}, // Delivery to the North Sanctum
        {8896, "TBC quest"}, // The Dwarven Spy
        {236, "Wothlk quest"}, // Fueling the Demolishers
        {7702, "Wothlk quest"}, // Kill 'Em With Sleep Deprivation
    };

    private static readonly List<Regex> NotAvailableNameRegexIdentifier = new List<Regex>()
    {
        new Regex("test.*quest", RegexOptions.IgnoreCase),
    };

    private static readonly HashSet<int> ManuallyTaggedRepeatable = new HashSet<int>()
    {
        996,  // Corrupted Windblossom 
        998,  // Corrupted Windblossom 
        1514, // Corrupted Windblossom 
        4115, // Corrupted Windblossom
        4221, // Corrupted Windblossom
        4222, // Corrupted Windblossom
        4343, // Corrupted Windblossom
        4403, // Corrupted Windblossom
        4466, // Corrupted Windblossom
        4467, // Corrupted Windblossom
        2523, // Corrupted Songflower
        2878, // Corrupted Songflower
        3363, // Corrupted Songflower
        4113, // Corrupted Songflower
        4114, // Corrupted Songflower
        4116, // Corrupted Songflower
        4118, // Corrupted Songflower
        4401, // Corrupted Songflower
        4464, // Corrupted Songflower
        4465, // Corrupted Songflower
        4117, // Corrupted Whipper Root
        4443, // Corrupted Whipper Root
        4444, // Corrupted Whipper Root
        4445, // Corrupted Whipper Root
        4446, // Corrupted Whipper Root
        4461, // Corrupted Whipper Root
        4119, // Corrupted Night Dragon
        4447, // Corrupted Night Dragon
        4448, // Corrupted Night Dragon
        4462, // Corrupted Night Dragon
        2881, // Troll Necklace Bounty repeatable version of 2880
        // PVP Quests
        8389, // Battle of Warsong Gulch lvl 19
        8431, // Battle of Warsong Gulch lvl 29
        8432, // Battle of Warsong Gulch lvl 39
        8433, // Battle of Warsong Gulch lvl 49
        8434, // Battle of Warsong Gulch lvl 59
        8435, // Battle of Warsong Gulch lvl 60
        8386, // Fight for Warsong Gulch lvl 19
        8404, // Fight for Warsong Gulch lvl 29
        8405, // Fight for Warsong Gulch lvl 39
        8406, // Fight for Warsong Gulch lvl 49
        8407, // Fight for Warsong Gulch lvl 59
        8408, // Fight for Warsong Gulch lvl 60
        8384, // Claiming Arathi Basin lvl 29
        8391, // Claiming Arathi Basin lvl 39
        8392, // Claiming Arathi Basin lvl 49
        8397, // Claiming Arathi Basin lvl 59
        8398, // Claiming Arathi Basin lvl 60
        8390, // Conquering Arathi Basin lvl 29
        8440, // Conquering Arathi Basin lvl 39
        8441, // Conquering Arathi Basin lvl 49
        8442, // Conquering Arathi Basin lvl 59
        8443, // Conquering Arathi Basin lvl 60
        8387, // Invaders of Alterac Valley
        8383, // Remember Alterac Valley!
        8385, // Concerted Efforts
        8388, // For Great Honor
        8493, // The Alliance Needs More Copper Bars!
        8495, // The Alliance Needs More Iron Bars!
        8500, // The Alliance Needs More Thorium Bars!
        8504, // The Alliance Needs More Stranglekelp!
        8506, // The Alliance Needs More Purple Lotus!
        8510, // The Alliance Needs More Arthas' Tears!
        8512, // The Alliance Needs More Light Leather!
        8514, // The Alliance Needs More Medium Leather!
        8516, // The Alliance Needs More Thick Leather!
        8518, // The Alliance Needs More Linen Bandages!
        8521, // The Alliance Needs More Silk Bandages!
        8523, // The Alliance Needs More Runecloth Bandages!
        8525, // The Alliance Needs More Rainbow Fin Albacore!
        8527, // The Alliance Needs More Roast Raptor!
        8529, // The Alliance Needs More Spotted Yellowtail!
        8531, // The Alliance Needs More Singed Corestones!
        8533, // The Horde Needs More Copper Bars!
        8543, // The Horde Needs More Tin Bars!
        8546, // The Horde Needs More Mithril Bars!
        8550, // The Horde Needs More Peacebloom!
        8581, // The Horde Needs More Firebloom!
        8583, // The Horde Needs More Purple Lotus!
        8589, // The Horde Needs More Heavy Leather!
        8591, // The Horde Needs More Thick Leather!
        8601, // The Horde Needs More Rugged Leather!
        8605, // The Horde Needs More Wool Bandages!
        8608, // The Horde Needs More Mageweave Bandages!
        8610, // The Horde Needs More Runecloth Bandages!
        8612, // The Horde Needs More Lean Wolf Steaks!
        8614, // The Horde Needs More Spotted Yellowtail!
        8616, // The Horde Needs More Baked Salmon!
        8618, // The Horde Needs More Singed Corestones!
        // AQ Opening silithus quests
        8302, // The Hand of the Righteous (AQ scepter quest)
        8507, // Field Duty - Alliance
        8731, // Field Duty - Horde
        // AQ Tactics
        8535, // Hoary Templar
        8536, // Earthen Templar
        8537, // Crimson Templar
        8737, // Azure Templar
        8538, // The Four Dukes
        8498, // Twilight Battle Orders
        8740, // Twilight Marauders
        8739, // Hive'Ashi Scout Report
        8738, // Hive'Regal Scout Report
        8534, // Hive'Zora Scout Report
        // AQ Combat
        8501, // Target: Hive'Ashi Stingers
        8502, // Target: Hive'Ashi Workers
        8770, // Target: Hive'Ashi Defenders
        8771, // Target: Hive'Ashi Sandstalkers
        8539, // Target: Hive'Zora Hive Sisters
        8687, // Target: Hive'Zora Tunnelers
        8772, // Target: Hive'Zora Waywatchers
        8773, // Target: Hive'Zora Reavers
        8774, // Target: Hive'Regal Ambushers
        8775, // Target: Hive'Regal Spitfires
        8776, // Target: Hive'Regal Slavemakers
        8777, // Target: Hive'Regal Burrowers
        // AQ Logistics
        8780, // Armor Kits for the Field - Alliance
        8787, // Armor Kits for the Field - Horde
        8781, // Arms for the Field - Alliance
        8786, // Arms for the Field - Horde
        8496, // Bandages for the Field - Alliance
        8810, // Bandages for the Field - Horde
        8540, // Boots for the Guard - Alliance
        8805, // Boots for the Guard - Horde
        8804, // Desert Survival Kits - Horde
        8497, // Desert Survival Kits - Alliance
        8783, // Extraordinary Materials - Alliance
        8809, // Extraordinary Materials - Horde
        8541, // Grinding Stones for the Guard - Alliance
        8806, // Grinding Stones for the Guard - Horde
        8779, // Scrying Materials - Alliance
        8807, // Scrying Materials - Horde
        8782, // Uniform Supplies - Alliance
        8808, // Uniform Supplies - Horde
        8778, // The Ironforge Brigade Needs Explosives!
        8785, // The Orgrimmar Legion Needs Mojo!
        8829, // The Ultimate Deception
        // Naxxramas Craftman quests
        9178, // Craftsman's Writ - Dense Weightstone
        9179, // Craftsman's Writ - Imperial Plate Chest
        9181, // Craftsman's Writ - Volcanic Hammer
        9182, // Craftsman's Writ - Huge Thorium Battleaxe
        9183, // Craftsman's Writ - Radiant Circlet
        9184, // Craftsman's Writ - Wicked Leather Headband
        9185, // Craftsman's Writ - Rugged Armor Kit
        9186, // Craftsman's Writ - Wicked Leather Belt
        9187, // Craftsman's Writ - Runic Leather Pants
        9188, // Craftsman's Writ - Brightcloth Pants
        9190, // Craftsman's Writ - Runecloth Boots
        9191, // Craftsman's Writ - Runecloth Bag
        9194, // Craftsman's Writ - Runecloth Robe
        9195, // Craftsman's Writ - Goblin Sapper Charge
        9196, // Craftsman's Writ - Thorium Grenade
        9197, // Craftsman's Writ - Gnomish Battle Chicken
        9198, // Craftsman's Writ - Thorium Tube
        9200, // Craftsman's Writ - Major Mana Potion
        9201, // Craftsman's Writ - Greater Arcane Protection Potion
        9202, // Craftsman's Writ - Major Healing Potion
        9203, // Craftsman's Writ - Flask of Petrification
        9204, // Craftsman's Writ - Stonescale Eel
        9205, // Craftsman's Writ - Plated Armorfish
        9206, // Craftsman's Writ - Lightning Eel
        // Naxxramas Necrotic runes quest
        9317, // Consecrated Sharpening Stones - Alliance
        9335, // Consecrated Sharpening Stones - Horde
        9318, // Blessed Wizard Oil - Alliance
        9334, // Blessed Wizard Oil - Horde
        9320, // Major Mana Potion - Horde
        9337, // Major Mana Potion - Alliance
        9321, // Major Healing Potion - Alliance
        9336, // Major Healing Potion - Horde
        9094, // Argent Dawn Gloves - Alliance
        9333, // Argent Dawn Gloves - Horde
        9341, // Tabard of the Argent Dawn - Alliance
        9343, // Tabard of the Argent Dawn - Horde
        9386, // A Light in Dark Places repeatable version of 9319
    };

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
        var identifier = NotAvailableNameIdentifiers.Find(identifier =>
            questName.Contains(identifier, StringComparison.InvariantCulture));
        if (identifier != null && !isException)
        {
            return new Quest {Id = id, Name = questName, ErrorMessage = $"questName has identifier {identifier}"};
        }


        var regexIdentifier = NotAvailableNameRegexIdentifier.Find(regex => regex.IsMatch(questName));
        if (regexIdentifier != null && !isException)
        {
            return new Quest
            {
                Id = id, Name = questName,
                ErrorMessage = $"questName has identifier {regexIdentifier.ToString().Replace(".*", " ")}"
            };
        }

        if (NotAvailableQuests.TryGetValue(id, out var reason))
        {
            return new Quest {Id = id, Name = questName, ErrorMessage = reason};
        }

        // if (html.Contains("This item is not available to players.") && !isException)
        // {
        //     return new Quest {Id = id, Name = questName, ErrorMessage = "item is not available to players"};
        // }

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
        var quickFacts = htmlDocument.DocumentNode.SelectSingleNode(".//table[@class='infobox']")?
            .SelectSingleNode(".//tr/td/script[contains(normalize-space(.), 'WH.markup.printHtml(\"[ul][li]Level:')]");
        if (quickFacts != null)
        {
            var levelMatch = new Regex("Level: (\\d+)", RegexOptions.Compiled).Match(quickFacts.InnerText);
            if (levelMatch.Success)
            {
                level = int.Parse(levelMatch.Groups[1].Value);
            }

            var requiredLevelMatch =
                new Regex("Requires level (\\d+)", RegexOptions.Compiled).Match(quickFacts.InnerText);
            if (requiredLevelMatch.Success)
            {
                requiredLevel = int.Parse(requiredLevelMatch.Groups[1].Value);
            }

            isRepeatable = quickFacts.InnerText.Contains("[li]Repeatable");
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
            IsRepeatable = isRepeatable,
            MoneyTurnIn = turnInMoney,
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