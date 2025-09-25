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

                var headerBuilder = new StringBuilder();
                headerBuilder.AppendJoin("\t",
                    "id",
                    "name",
                    "level",
                    "requiredLevel",
                    "minLevel",
                    "maxLevel"
                );
                
                for (int i = 1; i <= 60; i++)
                {
                    headerBuilder.Append($"\txpAt{i}");
                }
                for (int i = 1; i <= 60; i++)
                {
                    headerBuilder.Append($"\tmoneyAt{i}");
                }
                headerBuilder.Append("\txpToMoneyAt60");
                headerBuilder.Append("\ttotalMoneyAt60");
                for (int i = 1; i <= 10; i++)
                {
                    headerBuilder.AppendJoin("\t",
                        "",
                        $"reputationId{i}",
                        $"reputationName{i}",
                        $"reputationAmount{i}");
                }
                
                await availableStream.WriteLineAsync(headerBuilder.ToString());
                await notAvailableStream.WriteLineAsync("id\tname\treason");
                
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int id = 1; id <= itemsToProcess; id++)
                {
                    var quest = await questGetter.GetQuest(id);
                    if (quest.IsAvailable)
                    {
                        var rowBuilder = new StringBuilder();
                        rowBuilder.AppendJoin("\t",
                            quest.Id, 
                            quest.Name,
                            quest.Level,
                            quest.RequiredLevel,
                            quest.MinLevel,
                            quest.MaxLevel
                        );

                        var minLevel = Math.Min(quest.MinLevel, 60);
                        var maxLevel = Math.Min(quest.MaxLevel, 60);

                        if (quest.Experience.Count == 0)
                        {
                            for (int i = 1; i <= 60; i++)
                            {
                                rowBuilder.Append("\t");
                            }
                        }
                        else
                        {
                            for (int i = 1; i < quest.RequiredLevel; i++)
                            {
                                rowBuilder.Append("\t");
                            }
                            for (int i = quest.RequiredLevel; i < minLevel; i++)
                            {
                                rowBuilder.Append($"\t{quest.Experience[minLevel]}");
                            }
                            for (int i = minLevel; i <= maxLevel; i++)
                            {
                                rowBuilder.Append($"\t{quest.Experience[i]}");
                            }

                            for (int i = maxLevel + 1; i <= 60; i++)
                            {
                                rowBuilder.Append($"\t{quest.Experience[maxLevel]}");
                            }
                        }
                        
                        if (quest.Money.QuestReward.Count == 0)
                        {
                            for (int i = 1; i <= 60; i++)
                            {
                                rowBuilder.Append("\t");
                            }
                        }
                        else
                        {
                            for (int i = 1; i < quest.RequiredLevel; i++)
                            {
                                rowBuilder.Append("\t");
                            }
                            for (int i = quest.RequiredLevel; i < minLevel; i++)
                            {
                                rowBuilder.Append($"\t{quest.Money.QuestReward[minLevel]}");
                            }
                            for (int i = minLevel; i <= maxLevel; i++)
                            {
                                rowBuilder.Append($"\t{quest.Money.QuestReward[i]}");
                            }

                            for (int i = maxLevel + 1; i <= 60; i++)
                            {
                                rowBuilder.Append($"\t{quest.Money.QuestReward[maxLevel]}");
                            }
                        }
                        
                        rowBuilder.Append($"\t{quest.Money.ExperienceToMoney}");

                        var flatMoney = 0;
                        if (quest.Money.QuestReward.Count > 0)
                        {
                            flatMoney = quest.Money.QuestReward[quest.Money.QuestReward.Keys.Max()];
                        }
                        rowBuilder.Append($"\t{flatMoney + quest.Money.ExperienceToMoney}");
                        
                        foreach (var reputation in quest.Reputations)
                        {
                            rowBuilder.AppendJoin("\t",
                                "",
                                reputation.Id,
                                reputation.Name,
                                reputation.Amount
                            );
                        }
                        await availableStream.WriteLineAsync(rowBuilder.ToString());
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