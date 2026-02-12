using System.Text.RegularExpressions;

namespace WowheadScraper;

public class ItemLists
{
    public static readonly List<string> NotAvailableNameIdentifiers = new List<string>()
    {
        "OLD",
        "(old)",
        "DEBUG",
        "Deprecated",
        "Deprecate",
        "Depricated", // misspelled item
        "Deptecated", // misspelled item
        "DEPRECATED",
        "[DEP]",
        "DEP",
        "(DND)",
        "Monster",
        "[PH]",
        "PH",
        "QA",
        "(test)",
        "(Test)",
        "(TEST)",
        "Test",
        "TEST",
        "Unused",
        "<UNUSED>",
        "[UNUSED]",
        "UNUSED",
    };

    public static readonly List<Regex> NotAvailableQuickFactsIdentifier = new List<Regex>()
    {
        new Regex("Added in patch.*Season of Discovery"),
        new Regex("Deprecated"),
    };

    public static readonly List<int> NotAvailableExceptions = new List<int>()
    {
        16110, // Recipe: Monster Omelet - Prevents the check that matches all Monster items
        12218, // Monster Omelet - Prevents the check that matches all Monster items
        8523, // Field Testing Kit - Prevents the check that matches all Test items
        15102, // Un'Goro Tested Sample - Incorrectly marked as Not Available to players even though it is
        15103, // Corrupt Tested Sample - Incorrectly marked as Not Available to players even though it is
        5108, // Dark Iron Leather - Marked as Deprecated but still drops
    };

}