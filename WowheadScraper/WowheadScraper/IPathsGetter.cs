namespace WowheadScraper;

public interface IPathsGetter
{
    public int LastId { get; }
    public string AvailableTsvFilePath { get; }
    public string NotAvailableTsvFilePath { get; }
    public Uri GetUri(int id);
    public string GetHtmlFolderPath();
    public string GetHtmlFilePath(int id);
}