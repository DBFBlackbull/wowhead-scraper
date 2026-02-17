using System.Net;
using HtmlAgilityPack;

namespace WowheadScraper;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int SellPrice { get; set; }
    public string ErrorMessage { get; set; }
    public bool IsAvailable => string.IsNullOrWhiteSpace(ErrorMessage);
    
    public static async Task<Item> GetItem(int id, IItemSetup setup)
    {
        var html = await File.ReadAllTextAsync(setup.GetHtmlFilePath(id));
        
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        var itemName = htmlDocument.DocumentNode.SelectSingleNode(".//h1[@class='heading-size-1']")?.InnerText;
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return new Item {Id = id, ErrorMessage = "itemName was empty"};
        }

        itemName = WebUtility.HtmlDecode(itemName);
        if (itemName == setup.NotFoundName)
        {
            return new Item {Id = id, ErrorMessage = "item not found"};
        }

        var identifier = ItemLists.NotAvailableNameIdentifiers.Find(identifier =>
            itemName.Contains(identifier, StringComparison.InvariantCulture));
        var isException = ItemLists.NotAvailableExceptions.Contains(id);
        if (identifier != null && !isException)
        {
            return new Item {Id = id, Name = itemName, ErrorMessage = $"itemName has identifier {identifier}"};
        }

        var quickFacts = htmlDocument.DocumentNode.SelectSingleNode(".//table[@class='infobox after-buttons']")?.InnerHtml;
        if (quickFacts != null)
        {
            var regex = ItemLists.NotAvailableQuickFactsIdentifier.Find(regex =>
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