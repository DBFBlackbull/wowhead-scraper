namespace WowheadScraper;

public interface IPathsGetter
{
    public int LastId { get; }
    public string TsvFolderPath { get; }
    public string AvailableTsvFilePath { get; }
    public string NotAvailableTsvFilePath { get; }
    public Uri GetUri(int id);
    public string HtmlFolderPath { get; }
    public string GetHtmlFilePath(int id);
}