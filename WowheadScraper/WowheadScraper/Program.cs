using System.Net;
using HtmlAgilityPack;

namespace WowheadScraper;

class Program
{
    private static readonly Uri BaseUrl = new Uri("https://www.wowhead.com/", UriKind.Absolute);
    public static readonly HttpClient HttpClient = new HttpClient();
    private const int LastItemIdInClassic = 24283;

    public static readonly List<string> NotAvailableIdentifiers = new List<string>()
    {
        "OLD",
        "DEBUG",
        "Deprecated",
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

    public static readonly List<int> NotAvailableExceptions = new List<int>()
    {
        16110, // Recipe: Monster Omelet
        12218, // Monster Omelet
        8523, // Field Testing Kit
        15102, // Un'Goro Tested Sample - Incorrectly marked as Not Available to players even though it is
        15103 // Corrupt Tested Sample - Incorrectly marked as Not Available to players even though it is
    };

    static async Task Main(string[] args)
    {
        HttpClient.BaseAddress = BaseUrl;
        var folder = Path.Join(SolutionDirectory(), "classic");
        Directory.CreateDirectory(folder);

        var wowheadScraper = new ConsumerRunner();
        await wowheadScraper.Run(LastItemIdInClassic);
    }

    public static Item GetItem(int i, string html)
    {
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        var itemName = htmlDocument.DocumentNode.SelectSingleNode(".//h1[@class='heading-size-1']")?.InnerText;
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return new Item {ErrorMessage = $"{i}: itemName was empty"};
        }

        itemName = WebUtility.HtmlDecode(itemName);
        if (itemName == "Classic Items")
        {
            return new Item {ErrorMessage = $"{i}: item not found"};
        }

        var identifier = Program.NotAvailableIdentifiers.Find(identifier =>
            itemName.Contains(identifier, StringComparison.InvariantCulture));
        var isException = Program.NotAvailableExceptions.Contains(i);
        if (identifier != null && !isException)
        {
            return new Item {ErrorMessage = $"{i}: itemName has identifier {identifier}: {itemName}"};
        }

        if (html.Contains("This item is not available to players.") && !isException)
        {
            return new Item {ErrorMessage = $"{i}: item is not available to players: {itemName}"};
        }

        var sellPrice = 0;
        var sellPriceElement = htmlDocument.DocumentNode.SelectSingleNode(".//div[@class='whtt-sellprice']");
        if (sellPriceElement != null)
        {
            var gold = sellPriceElement.SelectSingleNode(".//span[@class='moneygold']")?.InnerText;
            var silver = sellPriceElement.SelectSingleNode(".//span[@class='moneysilver']")?.InnerText;
            var copper = sellPriceElement.SelectSingleNode(".//span[@class='moneycopper']")?.InnerText;

            sellPrice = GetMoney(gold, 1000) + GetMoney(silver, 100) + GetMoney(copper, 1);
        }

        return new Item {Name = itemName, SellPrice = sellPrice};
    }

    public static int GetMoney(string? money, int factor)
    {
        if (string.IsNullOrWhiteSpace(money))
        {
            return 0;
        }

        return int.Parse(money) * factor;
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