using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WowheadScraper;

class Program
{
    private static readonly Uri BaseUrl = new Uri("https://www.wowhead.com/", UriKind.Absolute);
    public static readonly HttpClient HttpClient = new HttpClient();
    public static readonly string TsvFolder = Path.Join(SolutionDirectory(), "tsv-files");

    public static readonly List<string> NotAvailableNameIdentifiers = new List<string>()
    {
        "OLD",
        "(old)",
        "DEBUG",
        "Deprecated",
        "Deprecate",
        "Depricated", // misspelled item
        "Deptecated", // misspelled item
        "DEPRECATED",
        "[DEP]",
        "DEP",
        "(DND)",
        "Monster",
        "[PH]",
        "PH",
        "QA",
        "(test)",
        "(Test)",
        "(TEST)",
        "Test",
        "TEST",
        "Unused",
        "<UNUSED>",
        "[UNUSED]",
        "UNUSED",
    };

    public static readonly List<Regex> NotAvailableQuickFactsIdentifier = new List<Regex>()
    {
        new Regex("Added in patch.*Season of Discovery"),
        new Regex("Deprecated"),
    };

    static async Task Main(string[] args)
    {
        HttpClient.BaseAddress = BaseUrl;

        // var wowheadScraper = new OrderedItemProducerConsumer();
        // await wowheadScraper.Run(40);
        var htmlProducer = new HtmlProducer();
        await htmlProducer.Run(40, Quest.LastQuestIdInClassic, Quest.HtmlFolderPath, "quest");
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
        if (key % 100 == 0)
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
}