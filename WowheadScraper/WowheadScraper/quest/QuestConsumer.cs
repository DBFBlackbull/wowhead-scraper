using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Text;

namespace WowheadScraper;

public class QuestConsumer
{
    public static async Task Run(IQuestGetter questGetter, int itemsToProcess = Quest.LastIdInClassic)
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
                headers.Add("xpToMoneyAt60");
                headers.Add("totalMoneyAt60");
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
                    var quest = await questGetter.GetQuest(id);
                    if (quest.IsAvailable)
                    {
                        var row = new List<string>
                        {
                            quest.Id.ToString(), 
                            quest.Name,
                            quest.Level.ToString(),
                            quest.RequiredLevel.ToString(),
                            quest.MinLevel.ToString(),
                            quest.MaxLevel.ToString()
                        };

                        for (int i = 1; i <= 60; i++)
                        {
                            if (quest.Experience.TryGetValue(i, out var xp))
                            {
                                row.Add(xp.ToString());
                            }
                            else
                            {
                                row.Add("");
                            }
                        }
                        
                        for (int i = 1; i <= 60; i++)
                        {
                            if (quest.Money.QuestReward.TryGetValue(i, out var coins))
                            {
                                row.Add(coins.ToString());
                            }
                            else
                            {
                                row.Add("");
                            }
                        }
                        
                        row.Add(quest.Money.ExperienceToMoney.ToString());

                        var totalMoneyAt60 = quest.Money.ExperienceToMoney;
                        if (quest.Money.QuestReward.Count > 0)
                        {
                            totalMoneyAt60 += quest.Money.QuestReward[quest.Money.QuestReward.Keys.Max()];
                        }
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
}