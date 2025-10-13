using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WowheadScraper;

public class Item : IHtmlProducerPaths
{
    public const int LastIdInClassic = 24283;
    public static readonly string HtmlFolderPath = Path.Join(Program.SolutionDirectory(), "classic", "items");
    public static readonly string AvailableTsvFilePath = Path.Join(Program.TsvFolderPath, "items-available.tsv");
    public static readonly string NotAvailableTsvFilePath = Path.Join(Program.TsvFolderPath, "items-not-available.tsv");
    private static string HtmlFilePath(int id) => Path.Join(HtmlFolderPath, $"item-{id}.html");
    
    public Uri GetUri(int id) => new Uri($"classic/item={id}", UriKind.Relative);
    public string GetHtmlFolderPath() => HtmlFolderPath;
    public string GetHtmlFilePath(int id) => HtmlFilePath(id);
    
    public int Id { get; set; }
    public string Name { get; set; }
    public int SellPrice { get; set; }
    public string ErrorMessage { get; set; }
    
    public bool IsAvailable => string.IsNullOrWhiteSpace(ErrorMessage);
    
    private static readonly List<string> NotAvailableNameIdentifiers = new List<string>()
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

    private static readonly List<Regex> NotAvailableQuickFactsIdentifier = new List<Regex>()
    {
        new Regex("Added in patch.*Season of Discovery"),
        new Regex("Deprecated"),
    };
    
    private static readonly List<int> NotAvailableExceptions = new List<int>()
    {
        16110, // Recipe: Monster Omelet
        12218, // Monster Omelet
        8523, // Field Testing Kit
        15102, // Un'Goro Tested Sample - Incorrectly marked as Not Available to players even though it is
        15103, // Corrupt Tested Sample - Incorrectly marked as Not Available to players even though it is
        5108, // Dark Iron Leather - Marked as Deprecated but still drops
    };
    
    public static async Task<Item> GetItem(int id)
    {
        var html = await File.ReadAllTextAsync(HtmlFilePath(id));
        
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        var title = htmlDocument.DocumentNode.SelectSingleNode(".//title");
        var h1 = htmlDocument.DocumentNode.SelectSingleNode(".//h1");
        if (title?.InnerText == "ERROR: The request could not be satisfied" || h1?.InnerText == "504 Gateway Timeout ERROR")
        {
            throw new Exception("Error reading from wowhead.com. Fetch HTML again.");
        }
        
        var itemName = htmlDocument.DocumentNode.SelectSingleNode(".//h1[@class='heading-size-1']")?.InnerText;
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return new Item {Id = id, ErrorMessage = "itemName was empty"};
        }

        itemName = WebUtility.HtmlDecode(itemName);
        if (itemName == "Classic Items")
        {
            return new Item {Id = id, ErrorMessage = "item not found"};
        }

        var identifier = NotAvailableNameIdentifiers.Find(identifier =>
            itemName.Contains(identifier, StringComparison.InvariantCulture));
        var isException = NotAvailableExceptions.Contains(id);
        if (identifier != null && !isException)
        {
            return new Item {Id = id, Name = itemName, ErrorMessage = $"itemName has identifier {identifier}"};
        }

        var quickFacts = htmlDocument.DocumentNode.SelectSingleNode(".//table[@class='infobox after-buttons']")?.InnerHtml;
        if (quickFacts != null)
        {
            var regex = NotAvailableQuickFactsIdentifier.Find(regex =>
                regex.IsMatch(quickFacts));
            if (regex != null && !isException)
            {
                return new Item {Id = id, Name = itemName, ErrorMessage = $"item quick facts has identifier {regex.ToString().Replace(".*", " ")}"};
            }
        }
        
        if (html.Contains("This item is not available to players.") && !isException)
        {
            return new Item {Id = id, Name = itemName, ErrorMessage = "item is not available to players"};
        }

        var sellPrice = 0;
        var sellPriceElement = htmlDocument.DocumentNode.SelectSingleNode(".//div[@class='whtt-sellprice']");
        if (sellPriceElement != null)
        {
            var gold = sellPriceElement.SelectSingleNode(".//span[@class='moneygold']")?.InnerText;
            var silver = sellPriceElement.SelectSingleNode(".//span[@class='moneysilver']")?.InnerText;
            var copper = sellPriceElement.SelectSingleNode(".//span[@class='moneycopper']")?.InnerText;

            sellPrice = Program.GetMoney(gold, 10000) + Program.GetMoney(silver, 100) + Program.GetMoney(copper, 1);
        }

        return new Item {Id = id, Name = itemName, SellPrice = sellPrice};
    }
}