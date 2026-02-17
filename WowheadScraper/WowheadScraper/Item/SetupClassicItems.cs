using System.Text.RegularExpressions;

namespace WowheadScraper;

public class SetupClassicItems : IItemSetup
{
    public int LastId => 24283;
    public string ExpansionPath => "classic";
    public string NotFoundName => "Classic Items";

    public string TsvFolderPath => Path.Join(Program.TsvFolderPath, ExpansionPath);
    public string AvailableTsvFilePath => Path.Join(TsvFolderPath, "items-available.tsv");
    public string NotAvailableTsvFilePath => Path.Join(TsvFolderPath, "items-not-available.tsv");
    
    public Uri GetUri(int id) => new Uri($"{ExpansionPath}/item={id}", UriKind.Relative);

    public string HtmlFolderPath => Path.Join(Program.SolutionDirectory(), ExpansionPath, "items");
    public string GetHtmlFilePath(int id) => Path.Join(HtmlFolderPath, $"item-{id}.html");
}