namespace WowheadScraper;

public class Item
{
    public string Name { get; set; }
    public int SellPrice { get; set; }
    public string ErrorMessage { get; set; }

    public bool IsAvailable => string.IsNullOrWhiteSpace(ErrorMessage);
}