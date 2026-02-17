using System.Diagnostics;

namespace WowheadScraper;

public class QuestConsumer
{
    public static async Task Run(ITaskGetter<Quest> questGetter, IQuestSetup setup)
    {
        Directory.CreateDirectory(Program.TsvFolderPath);

        var totalStopwatch = new Stopwatch();
        totalStopwatch.Start();
        Console.WriteLine($"Starting consuming {setup.LastId} items...");
        
        await using (var availableStream = new StreamWriter(File.Create(setup.AvailableTsvFilePath)))
        {
            availableStream.AutoFlush = true;
            await GenerateAvailableQuestHeaders(availableStream, setup);
            
            await using (var notAvailableStream = new StreamWriter(File.Create(setup.NotAvailableTsvFilePath)))
            {
                notAvailableStream.AutoFlush = true;
                await GenerateNotAvailableQuestHeaders(notAvailableStream);

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int id = 1; id <= setup.LastId; id++)
                {
                    var quest = await questGetter.GetTask(id);
                    if (quest.IsAvailable)
                    {
                        var row = new List<string>
                        {
                            quest.Id.ToString(), 
                            quest.Name,
                            quest.Level.ToString(),
                            quest.RequiredLevel.ToString(),
                            quest.Type,
                            quest.IsBattleground.HasValue ? quest.IsBattleground.Value.ToString() : "",
                            quest.Faction,
                            quest.IsRepeatable.ToString(),
                            quest.IsManuallyRepeatableOverride.HasValue ? quest.IsManuallyRepeatableOverride.Value.ToString() : "",
                            quest.MinLevel.ToString(),
                            quest.MaxLevel.ToString()
                        };

                        for (int i = 1; i <= setup.MaxLevel; i++)
                        {
                            row.Add(quest.Experience.TryGetValue(i, out var xp) 
                                ? xp.ToString() 
                                : ""
                            );
                        }
                        
                        for (int i = 1; i <= setup.MaxLevel; i++)
                        {
                            row.Add(quest.Money.QuestReward.TryGetValue(i, out var coins) 
                                ? coins.ToString() 
                                : ""
                            );
                        }

                        var xpToMoneyAtMaxLevel = quest.Money.ExperienceToMoney;
                        row.Add(xpToMoneyAtMaxLevel.ToString());

                        var flatMoneyAtMaxLevel = 0; 
                        if (quest.Money.QuestReward.Count > 0)
                        {
                            // PvP quests go up to level 69 giving more gold with each level
                            // Cap at 60 to get the correct vanilla value
                            var key = Math.Min(quest.Money.QuestReward.Keys.Max(), setup.MaxLevel);
                            flatMoneyAtMaxLevel = quest.Money.QuestReward[key];
                        }

                        var xpToGoldStatus = XpToGoldStatus(quest, setup);
                        row.AddRange(
                            quest.RequiredMoney.ToString(),
                            xpToGoldStatus.Item1,
                            xpToGoldStatus.Item2
                        );
                        
                        var totalMoneyAtMaxLevel = flatMoneyAtMaxLevel + xpToMoneyAtMaxLevel;
                        row.Add(totalMoneyAtMaxLevel.ToString());
                        
                        foreach (var reputation in quest.Reputations)
                        {
                            row.AddRange(
                                reputation.Id.ToString(),
                                reputation.Name,
                                reputation.Amount.ToString()
                            );
                        }
                        await availableStream.WriteLineAsync(string.Join("\t", row));
                    }
                    else
                    {
                        var row = new List<string>()
                        {
                            quest.Id.ToString(),
                            quest.Name,
                            quest.ErrorMessage
                        };
                        await notAvailableStream.WriteLineAsync(string.Join("\t", row));
                    }

                    Program.LogProgress(id, setup.LastId, stopwatch);
                    // ------------------------------------
                }
            }
        }
        
        Program.LogJobDone("quests", totalStopwatch);
    }

    private static async Task GenerateNotAvailableQuestHeaders(StreamWriter notAvailableStream)
    {
        var notAvailableHeaders = new List<string>()
        {
            "id",
            "name",
            "reason"
        };
        await notAvailableStream.WriteLineAsync(string.Join("\t", notAvailableHeaders));
    }

    private static async Task GenerateAvailableQuestHeaders(StreamWriter availableStream, IQuestSetup setup)
    {
        var headers = new List<string>()
        {
            "id",
            "name",
            "level",
            "requiredLevel",
            "type",
            "manualIsBattleground",
            "faction",
            "wowheadRepeatable",
            "manualRepeatableOverride",
            "minLevel",
            "maxLevel"
        };
                
        for (int i = 1; i <= setup.MaxLevel; i++)
        {
            headers.Add($"xpAt{i}");
        }
        for (int i = 1; i <= setup.MaxLevel; i++)
        {
            headers.Add($"moneyAt{i}");
        }
        headers.AddRange(
            $"xpToMoneyAt{setup.MaxLevel}",
            "requiredMoney",
            $"goldAt{setup.MaxLevel}Status",
            $"goldAt{setup.MaxLevel}Note",
            $"calculatedTotalMoneyAt{setup.MaxLevel}"
        );
        for (int i = 1; i <= 4; i++)
        {
            headers.AddRange(
                $"reputationId{i}",
                $"reputationName{i}",
                $"reputationAmount{i}");
        }
                
        await availableStream.WriteLineAsync(string.Join("\t", headers));
    }

    private static Tuple<string, string> XpToGoldStatus(Quest quest, IQuestSetup setup)
    {
        var xpToMoneyAtMaxLevel = quest.Money.ExperienceToMoney;
        var xpAtMaxLevel = 0;
        if (quest.Experience.Count > 0)
        {
            var key = Math.Min(quest.Experience.Keys.Max(), setup.MaxLevel);
            xpAtMaxLevel = quest.Experience[key];
            xpAtMaxLevel = xpAtMaxLevel == 10 ? 0 : xpAtMaxLevel; // Fix for 3 level 1 quests giving 10 xp, but 0 at 60.
        }

        if (xpAtMaxLevel == 0)
        {
            return xpToMoneyAtMaxLevel == 0  
                ? new Tuple<string, string>("OK", "No xp, no gold")
                : new Tuple<string, string>("Validate", "No xp, but still gives gold");
        }

        if (quest.RequiredMoney > 0)
        {
            return xpToMoneyAtMaxLevel == 0 
                ? new Tuple<string, string>("OK", "Quests requiring gold, gives no gold") 
                : new Tuple<string, string>("Validate", "Quests requiring gold gives gold back");
        }
        
        if (quest.GetIsRepeatable())
        {
            return xpToMoneyAtMaxLevel == 0 
                ? new Tuple<string, string>("OK", "Repeatable quest gives no gold") 
                : new Tuple<string, string>("Validate", "Repeatable quest gives gold");
        }

        if (xpAtMaxLevel > 0)
        {
            return xpToMoneyAtMaxLevel > 0 
                ? new Tuple<string, string>("OK", "Quest gives gold at 60") 
                : new Tuple<string, string>("Validate", "Quest gives no gold at 60");
        }
        
        return new Tuple<string, string>("Unhandled", "");
    }
}