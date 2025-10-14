using System.Diagnostics;

namespace WowheadScraper;

public class QuestConsumer
{
    public static async Task Run(ITaskGetter<Quest> questGetter, int itemsToProcess = Quest.LastIdInClassic)
    {
        Directory.CreateDirectory(Program.TsvFolderPath);
        
        var totalStopwatch = new Stopwatch();
        totalStopwatch.Start();
        Console.WriteLine($"Starting consuming {itemsToProcess} items...");
        
        await using (var availableStream = new StreamWriter(File.Create(Quest.AvailableTsvFilePath)))
        {
            availableStream.AutoFlush = true;
            await using (var notAvailableStream = new StreamWriter(File.Create(Quest.NotAvailableTsvFilePath)))
            {
                notAvailableStream.AutoFlush = true;

                var headers = new List<string>()
                {
                    "id",
                    "name",
                    "level",
                    "requiredLevel",
                    "wowheadRepeatable",
                    "manualRepeatable",
                    "minLevel",
                    "maxLevel"
                };
                
                for (int i = 1; i <= 60; i++)
                {
                    headers.Add($"xpAt{i}");
                }
                for (int i = 1; i <= 60; i++)
                {
                    headers.Add($"moneyAt{i}");
                }
                headers.AddRange(
                    "xpToMoneyAt60",
                    "requiredMoney",
                    "goldAt60Status",
                    "goldAt60Note",
                    "calculatedTotalMoneyAt60"
                );
                for (int i = 1; i <= 10; i++)
                {
                    headers.AddRange(
                        $"reputationId{i}",
                        $"reputationName{i}",
                        $"reputationAmount{i}");
                }
                
                await availableStream.WriteLineAsync(string.Join("\t", headers));

                var notAvailableHeaders = new List<string>()
                {
                    "id",
                    "name",
                    "reason"
                };
                await notAvailableStream.WriteLineAsync(string.Join("\t", notAvailableHeaders));
                
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int id = 1; id <= itemsToProcess; id++)
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
                            quest.IsRepeatable.ToString(),
                            quest.IsManuallyTaggedRepeatable.ToString(),
                            quest.MinLevel.ToString(),
                            quest.MaxLevel.ToString()
                        };

                        for (int i = 1; i <= 60; i++)
                        {
                            row.Add(quest.Experience.TryGetValue(i, out var xp) 
                                ? xp.ToString() 
                                : ""
                            );
                        }
                        
                        for (int i = 1; i <= 60; i++)
                        {
                            row.Add(quest.Money.QuestReward.TryGetValue(i, out var coins) 
                                ? coins.ToString() 
                                : ""
                            );
                        }

                        var xpToMoneyAt60 = quest.Money.ExperienceToMoney;
                        row.Add(xpToMoneyAt60.ToString());

                        var flatMoneyAt60 = 0; 
                        if (quest.Money.QuestReward.Count > 0)
                        {
                            // PvP quests go up to level 69 giving more gold with each level
                            // Cap at 60 to get the correct vanilla value
                            var key = Math.Min(quest.Money.QuestReward.Keys.Max(), 60);
                            flatMoneyAt60 = quest.Money.QuestReward[key];
                        }

                        var xpToGoldStatus = XpToGoldStatus(quest);
                        row.AddRange(
                            quest.RequiredMoney.ToString(),
                            xpToGoldStatus.Item1,
                            xpToGoldStatus.Item2
                        );
                        
                        var totalMoneyAt60 = flatMoneyAt60 + xpToMoneyAt60;
                        row.Add(totalMoneyAt60.ToString());
                        
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

                    Program.LogProgress(id, itemsToProcess, stopwatch);
                    // ------------------------------------
                }
            }
        }
        
        Console.WriteLine();
        Console.WriteLine($"All quests consumed. Elapsed {totalStopwatch.Elapsed:g}");
    }

    private static Tuple<string, string> XpToGoldStatus(Quest quest)
    {
        var xpToMoneyAt60 = quest.Money.ExperienceToMoney;
        var xpAt60 = 0;
        if (quest.Experience.Count > 0)
        {
            var key = Math.Min(quest.Experience.Keys.Max(), 60);
            xpAt60 = quest.Experience[key];
        }

        if (xpAt60 == 0)
        {
            return xpToMoneyAt60 == 0  
                ? new Tuple<string, string>("OK", "No xp, no gold")
                : new Tuple<string, string>("Validate", "No xp, but still gives gold");
        }

        if (quest.RequiredMoney > 0)
        {
            return xpToMoneyAt60 == 0 
                ? new Tuple<string, string>("OK", "Quests requiring gold, gives no gold") 
                : new Tuple<string, string>("Validate", "Quests requiring gold gives gold back");
        }
        
        var isRepeatable = quest.IsRepeatable || quest.IsManuallyTaggedRepeatable;
        if (isRepeatable)
        {
            return xpToMoneyAt60 == 0 
                ? new Tuple<string, string>("OK", "Repeatable quest gives no gold") 
                : new Tuple<string, string>("Validate", "Repeatable quest gives gold");
        }

        if (xpAt60 > 0)
        {
            return xpToMoneyAt60 > 0 
                ? new Tuple<string, string>("OK", "Quest gives gold at 60") 
                : new Tuple<string, string>("Validate", "Quest gives no gold at 60");
        }
        
        return new Tuple<string, string>("Unhandled", "");
    }
}