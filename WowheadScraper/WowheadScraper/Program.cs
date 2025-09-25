using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WowheadScraper;

class Program
{
    private static readonly Uri BaseUrl = new Uri("https://www.wowhead.com/", UriKind.Absolute);
    public static readonly HttpClient HttpClient = new HttpClient();
    public static readonly string TsvFolderPath = Path.Join(SolutionDirectory(), "tsv-files");

    static async Task Main(string[] args)
    {
        HttpClient.BaseAddress = BaseUrl;

        // await new HtmlProducer().Run(40, Quest.LastIdInClassic, new Quest());
        
        await new OrderedQuestProducerConsumer().Run(40);

        //await QuestConsumer.Run(new HtmlQuestGetter());
    }

    public static int GetMoney(string? money, int factor)
    {
        if (string.IsNullOrWhiteSpace(money))
        {
            return 0;
        }

        return int.Parse(money) * factor;
    }

    public static void LogProgress(int key, int itemsToProcess, Stopwatch? stopwatch = null)
    {
        if (key % 100 == 0 || key == itemsToProcess)
        {
            var percentageComplete = key / (double)itemsToProcess * 100;
            var elapsedTime = "";
            if (stopwatch != null)
            {
                elapsedTime = $"Elapsed {stopwatch.Elapsed.Seconds} seconds";
                stopwatch.Restart();
            }
            Console.WriteLine($"[{DateTime.Now:t}] {percentageComplete,3:F0}% {key,5} / {itemsToProcess} completed. {elapsedTime}");
        }
    }

    public static string SolutionDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }

        return directory.FullName;
    }

    // public static void TestXpRounding(int xp)
    // {
    //     var decimals = xp.ToString().Length;
    //     Console.WriteLine($"{xp.ToString().PadLeft(decimals)} Full xp");
    //     Console.WriteLine($"{Quest.RoundQuestXp(xp*0.8m).ToString().PadLeft(decimals)} 0.8 xp");
    //     Console.WriteLine($"{Quest.RoundQuestXp(xp*0.6m).ToString().PadLeft(decimals)} 0.6 xp");
    //     Console.WriteLine($"{Quest.RoundQuestXp(xp*0.4m).ToString().PadLeft(decimals)} 0.4 xp");
    //     Console.WriteLine($"{Quest.RoundQuestXp(xp*0.2m).ToString().PadLeft(decimals)} 0.2 xp");
    //     Console.WriteLine($"{Quest.RoundQuestXp(xp*0.1m).ToString().PadLeft(decimals)} 0.1 xp");
    // }
}