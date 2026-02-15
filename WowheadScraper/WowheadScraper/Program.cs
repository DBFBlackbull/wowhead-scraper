using System.Diagnostics;
using System.Globalization;

namespace WowheadScraper;

class Program
{
    private static readonly Uri BaseUrl = new Uri("https://www.wowhead.com/", UriKind.Absolute);
    public static readonly HttpClient HttpClient = new HttpClient();
    public static readonly string TsvFolderPath = Path.Join(SolutionDirectory(), "tsv-files");

    static async Task Main(string[] args)
    {
        HttpClient.BaseAddress = BaseUrl;

        await new HtmlProducer().Run(40, new SetupClassicQuests());
        
        //await new OrderedProducerConsumer<Item>().Run(40, Item.LastIdInClassic, ItemProducer.Run, ItemConsumer.Run);
        // await new OrderedProducerConsumer<Quest>().Run(40, Quest.LastIdInClassic, QuestProducer.Run, QuestConsumer.Run);

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

    public static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now.ToString("T", CultureInfo.InvariantCulture)}] {message}");
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
            Log($"{percentageComplete,3:F0}% {key,5} / {itemsToProcess} completed. {elapsedTime}");
        }
    }

    public static void LogJobDone(string worker, Stopwatch stopwatch)
    {
        Console.WriteLine();
        Log($"All {worker} have finished their work. Elapsed {stopwatch.Elapsed:g}");
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