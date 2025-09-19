using System.Collections.Concurrent;
using System.Diagnostics;

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

                var questHeaders = string.Join("\t",
                    "id",
                    "name",
                    "level",
                    "experience",
                    "moneyFlatReward",
                    "moneyExperienceReward",
                    "moneyTotalReward",
                    "reputationId",
                    "reputationName",
                    "reputationAmount"
                );
                await availableStream.WriteLineAsync(questHeaders);
                await notAvailableStream.WriteLineAsync("id\tname\treason");
                
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int id = 1; id <= itemsToProcess; id++)
                {
                    var quest = await questGetter.GetQuest(id);
                    if (quest.IsAvailable)
                    {
                        var reputation = "";
                        if (quest.Reputations.Count > 0)
                        {
                            var questReputation = quest.Reputations[0];
                            reputation = string.Join("\t",
                                questReputation.Id,
                                questReputation.Name,
                                questReputation.Amount
                            );
                        }

                        var questProperties = string.Join("\t",
                            quest.Id, 
                            quest.Name,
                            quest.Experience.Level,
                            quest.Experience.Experience,
                            quest.Money.QuestReward,
                            quest.Money.ExperienceToMoney,
                            quest.Money.Total,
                            reputation
                        );
                        await availableStream.WriteLineAsync(questProperties);
                    }
                    else
                    {
                        await notAvailableStream.WriteLineAsync($"{quest.Id}\t{quest.Name}\t{quest.ErrorMessage}");
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