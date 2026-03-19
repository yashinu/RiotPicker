using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RiotPicker.Models;

namespace RiotPicker.Services;

public class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _configPath;
    public AppConfig Data { get; private set; } = new();

    public ConfigService()
    {
        _configPath = GetConfigPath();
        Load();
    }

    private static string GetConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrEmpty(appData))
        {
            var dir = Path.Combine(appData, "RiotPicker");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "config.json");
        }
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(exeDir, "config.json");
    }

    public void Load()
    {
        // Migrate old config from exe directory
        if (!File.Exists(_configPath))
        {
            var oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (oldPath != _configPath && File.Exists(oldPath))
            {
                File.Copy(oldPath, _configPath);
            }
        }

        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                Data = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppConfig) ?? new AppConfig();

                // Migrate old ban_champion -> ban_champions
                var node = JsonNode.Parse(json);
                if (node?["lol"]?["ban_champion"] is not null && Data.Lol.BanChampions.Count == 0)
                {
                    var old = node["lol"]!["ban_champion"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(old))
                        Data.Lol.BanChampions = [old];
                }

                // Ensure all roles exist
                foreach (var role in RoleData.Roles)
                {
                    Data.Lol.Roles.TryAdd(role, []);
                }
                return;
            }
            catch (JsonException) { }
        }

        Data = new AppConfig();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (dir != null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(Data, AppJsonContext.Default.AppConfig);
        File.WriteAllText(_configPath, json);
    }

    // LoL helpers
    public List<string> GetLolPicks(string role) =>
        Data.Lol.Roles.TryGetValue(role, out var list) ? list : [];

    public void SetLolPicks(string role, List<string> champions)
    {
        Data.Lol.Roles[role] = champions;
        Save();
    }

    public List<string> GetBanChampions() => Data.Lol.BanChampions;

    public void SetBanChampions(List<string> champions)
    {
        Data.Lol.BanChampions = champions;
        Save();
    }

    public bool GetAutoAccept() => Data.Lol.AutoAccept;

    public void SetAutoAccept(bool value)
    {
        Data.Lol.AutoAccept = value;
        Save();
    }

    public bool GetAutoRune() => Data.Lol.AutoRune;

    public void SetAutoRune(bool value)
    {
        Data.Lol.AutoRune = value;
        Save();
    }

    public bool GetLolEnabled() => Data.Lol.Enabled;

    public void SetLolEnabled(bool value)
    {
        Data.Lol.Enabled = value;
        Save();
    }

    // Valorant helpers
    public List<string> GetValorantAgents() => Data.Valorant.Agents;

    public void SetValorantAgents(List<string> agents)
    {
        Data.Valorant.Agents = agents;
        Save();
    }

    public bool GetValEnabled() => Data.Valorant.Enabled;

    public void SetValEnabled(bool value)
    {
        Data.Valorant.Enabled = value;
        Save();
    }

    public string GetLanguage() => Data.Language;

    public void SetLanguage(string lang)
    {
        Data.Language = lang;
        Save();
    }
}
