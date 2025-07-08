using System.Diagnostics;
using HtmlAgilityPack;

namespace WowheadScraper;

class Program
{
    private static readonly Uri BaseUrl = new Uri("https://www.wowhead.com/", UriKind.Absolute);
    private static readonly HttpClient HttpClient = new HttpClient();

    private static readonly List<string> NotAvailableIdentifiers = new List<string>()
    {
        "OLD",
        "Deprecated",
        "Depricated", // misspelled item
        "Deptecated", // misspelled item
        "DEBUG",
        "Monster",
        "QA",
        "(Test)",
        "[PH]",
    };

    private static readonly List<int> NotAvailableExceptions = new List<int>()
    {
        16110, // Recipe: Monster Omelet
        12218, // Monster Omelet
    };

    static async Task Main(string[] args)
    {
        HttpClient.BaseAddress = BaseUrl;
        
        var wowheadScraper = new OrderedProducerConsumer();
        await wowheadScraper.Run(10, 25000);
    }

    public static async Task<Item> GetItem(int i)
    {
        string html;
        using (var response = await HttpClient.GetAsync(new Uri($"classic/item={i}", UriKind.Relative)))
        {
            if (!response.IsSuccessStatusCode)
            {
                return new Item {ErrorMessage = $"{i}: failed to get item: {response.StatusCode}"};
            }

            if (response.RequestMessage?.RequestUri != null)
            {
                var request = response.RequestMessage.RequestUri;
                var query = System.Web.HttpUtility.ParseQueryString(request.Query);
                if (query["notFound"] != null)
                {
                    return new Item {ErrorMessage = $"{i}: item not found"};
                }
            }

            using (var responseContent = response.Content)
            {
                html = await responseContent.ReadAsStringAsync();
            }
        }

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        // TODO: Handle & and "
        var itemName = htmlDocument.DocumentNode.SelectSingleNode("//h1[@class='heading-size-1']")?.InnerText;
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return new Item {ErrorMessage = $"{i}: itemName was empty"};
        }

        var isDeprecated = NotAvailableIdentifiers.Any(identifier =>
            itemName.Contains(identifier, StringComparison.InvariantCulture));
        var isException = NotAvailableExceptions.Contains(i);
        if (isDeprecated && !isException)
        {
            return new Item {ErrorMessage = $"{i}: item is deprecated/old/monster: {itemName}"};
        }
        
        if (html.Contains("This item is not available to players."))
        {
            return new Item {ErrorMessage = $"{i}: item is not available to players: {itemName}"};
        }

        // TODO Fix recipe prices
        var sellPrice = 0;
        var sellPriceElement = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='whtt-sellprice']");
        if (sellPriceElement != null)
        {
            var gold = sellPriceElement.SelectSingleNode("//span[@class='moneygold']")?.InnerText;
            var silver = sellPriceElement.SelectSingleNode("//span[@class='moneysilver']")?.InnerText;
            var copper = sellPriceElement.SelectSingleNode("//span[@class='moneycopper']")?.InnerText;

            sellPrice = GetMoney(gold, 1000) + GetMoney(silver, 100) + GetMoney(copper, 1);
        }

        return new Item {Name = itemName, SellPrice = sellPrice};
    }

    private static int GetMoney(string? money, int factor)
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