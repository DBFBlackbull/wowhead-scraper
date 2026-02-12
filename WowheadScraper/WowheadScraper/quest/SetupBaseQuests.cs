namespace WowheadScraper;

public abstract class SetupBaseQuests
{
    public abstract string ExpansionPath { get; }

    public string AvailableTsvFilePath => Path.Join(Program.TsvFolderPath, ExpansionPath, "quests-available.tsv");
    public string NotAvailableTsvFilePath => Path.Join(Program.TsvFolderPath, ExpansionPath, "quests-not-available.tsv");

    public Uri GetUri(int id) => new Uri($"{ExpansionPath}/quest={id}", UriKind.Relative);
    public string GetHtmlFolderPath() => Path.Join(Program.SolutionDirectory(), ExpansionPath, "quests");
    public string GetHtmlFilePath(int id) => Path.Join(GetHtmlFolderPath(), $"quest-{id}.html");
}