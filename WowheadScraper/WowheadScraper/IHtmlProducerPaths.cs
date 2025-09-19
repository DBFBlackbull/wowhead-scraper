namespace WowheadScraper;

public interface IHtmlProducerPaths
{
    public Uri GetUri(int id);
    public string GetHtmlFolderPath();
    public string GetHtmlFilePath(int id);
}