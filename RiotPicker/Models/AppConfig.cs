using System.Text.Json.Serialization;

namespace RiotPicker.Models;

public class AppConfig
{
    [JsonPropertyName("lol")]
    public LolConfig Lol { get; set; } = new();

    [JsonPropertyName("valorant")]
    public ValorantConfig Valorant { get; set; } = new();

    [JsonPropertyName("language")]
    public string Language { get; set; } = "tr";
}

public class LolConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("auto_accept")]
    public bool AutoAccept { get; set; } = true;

    [JsonPropertyName("auto_rune")]
    public bool AutoRune { get; set; } = true;

    [JsonPropertyName("ban_champions")]
    public List<string> BanChampions { get; set; } = [];

    [JsonPropertyName("roles")]
    public Dictionary<string, List<string>> Roles { get; set; } = new()
    {
        ["TOP"] = [],
        ["JUNGLE"] = [],
        ["MIDDLE"] = [],
        ["BOTTOM"] = [],
        ["UTILITY"] = [],
    };
}

public class ValorantConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("agents")]
    public List<string> Agents { get; set; } = [];
}
