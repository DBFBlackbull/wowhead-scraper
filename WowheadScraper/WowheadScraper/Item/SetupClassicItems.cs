using System.Text.RegularExpressions;

namespace WowheadScraper;

public class SetupClassicItems : IItemSetup
{
    public int LastId => 24283;
    public string ExpansionPath => "classic";
    public string NotFoundName => "Classic Items";
    
    public string AvailableTsvFilePath => Path.Join(Program.TsvFolderPath, ExpansionPath, "items-available.tsv");
    public string NotAvailableTsvFilePath => Path.Join(Program.TsvFolderPath, ExpansionPath, "items-not-available.tsv");
    
    public Uri GetUri(int id) => new Uri($"{ExpansionPath}/item={id}", UriKind.Relative);
    public string GetHtmlFolderPath() => Path.Join(Program.SolutionDirectory(), ExpansionPath, "items");
    public string GetHtmlFilePath(int id) => Path.Join(GetHtmlFolderPath(), $"item-{id}.html");
}