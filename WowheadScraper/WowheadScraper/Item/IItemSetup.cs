using System.Text.RegularExpressions;

namespace WowheadScraper;

public interface IItemSetup : IPathsGetter
{
    public string NotFoundName { get; }
}