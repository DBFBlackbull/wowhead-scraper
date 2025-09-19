using System.Text.Json.Serialization;

namespace WowheadScraper;

public class QuestDetails
{
    [JsonPropertyName("minLevel")]
    public int MinLevel { get; set; }

    [JsonPropertyName("maxLevel")]
    public int MaxLevel { get; set; }

    [JsonPropertyName("xp")]
    public RewardProgression Xp { get; set; } = new();

    [JsonPropertyName("coin")]
    public CoinProgression Coin { get; set; } = new();

}

public class RewardProgression
{
    [JsonPropertyName("multiplier")]
    public int Multiplier { get; set; }

    [JsonPropertyName("levels")]
    public Dictionary<int, int> Levels { get; set; } = new();

    public int GetLevelsReward()
    {
        return Levels.Values.FirstOrDefault();
    }
}

public sealed class CoinProgression : RewardProgression
{
    [JsonPropertyName("rewardAtCap")]
    public int RewardAtCap { get; set; }
}
