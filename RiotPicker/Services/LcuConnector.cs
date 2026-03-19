using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RiotPicker.Models;

namespace RiotPicker.Services;

public partial class LcuConnector : BaseConnector
{
    private static readonly string[] CommonPaths =
    [
        @"C:\Riot Games\League of Legends\lockfile",
        @"D:\Riot Games\League of Legends\lockfile",
        @"E:\Riot Games\League of Legends\lockfile",
        @"F:\Riot Games\League of Legends\lockfile",
        @"C:\Games\League of Legends\lockfile",
        @"D:\Games\League of Legends\lockfile",
        @"C:\Program Files\Riot Games\League of Legends\lockfile",
        @"C:\Program Files (x86)\Riot Games\League of Legends\lockfile",
    ];

    public LcuConnector() : base("")
    {
    }

    public static bool IsLolClientRunning()
    {
        try
        {
            return Process.GetProcessesByName("LeagueClientUx").Length > 0;
        }
        catch { return false; }
    }

    public bool TryReconnect()
    {
        if (Connected) return true;

        // Strategy 1: Check common lockfile paths
        foreach (var path in CommonPaths)
        {
            if (File.Exists(path))
            {
                LockfilePath = path;
                if (ParseLockfile()) return true;
            }
        }

        // Strategy 2: PowerShell to get command line args
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -Command \"Get-CimInstance Win32_Process -Filter \\\"name='LeagueClientUx.exe'\\\" | Select-Object -ExpandProperty CommandLine\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(5000);

                var installMatch = InstallDirRegex().Match(output);
                if (installMatch.Success)
                {
                    var installDir = installMatch.Groups[1].Value.Trim().TrimEnd('\\');
                    var lockfile = Path.Combine(installDir, "lockfile");
                    if (File.Exists(lockfile))
                    {
                        LockfilePath = lockfile;
                        if (ParseLockfile()) return true;
                    }
                }

                var portMatch = AppPortRegex().Match(output);
                var tokenMatch = AuthTokenRegex().Match(output);
                if (portMatch.Success && tokenMatch.Success)
                {
                    Port = int.Parse(portMatch.Groups[1].Value);
                    Password = tokenMatch.Groups[1].Value;
                    Protocol = "https";
                    SetBasicAuth("riot", Password);
                    Connected = true;
                    return true;
                }
            }
        }
        catch { }

        Connected = false;
        return false;
    }

    // --- LCU API Endpoints ---

    public async Task<string?> GetGameflowPhaseAsync()
    {
        var resp = await GetAsync("/lol-gameflow/v1/gameflow-phase");
        if (resp?.IsSuccessStatusCode == true)
        {
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize(json, AppJsonContext.Default.String);
        }
        return null;
    }

    public async Task AcceptMatchAsync()
    {
        await PostAsync("/lol-matchmaking/v1/ready-check/accept");
    }

    public async Task<JsonElement?> GetChampSelectSessionAsync()
    {
        var resp = await GetAsync("/lol-champ-select/v1/session");
        if (resp?.IsSuccessStatusCode == true)
        {
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize(json, AppJsonContext.Default.JsonElement);
        }
        return null;
    }

    public async Task<List<int>> GetPickableChampionsAsync()
    {
        var resp = await GetAsync("/lol-champ-select/v1/pickable-champion-ids");
        if (resp?.IsSuccessStatusCode == true)
        {
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize(json, AppJsonContext.Default.ListInt32) ?? [];
        }
        return [];
    }

    public async Task<List<int>> GetBannableChampionsAsync()
    {
        var resp = await GetAsync("/lol-champ-select/v1/bannable-champion-ids");
        if (resp?.IsSuccessStatusCode == true)
        {
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize(json, AppJsonContext.Default.ListInt32) ?? [];
        }
        return [];
    }

    public async Task<bool> SetChampionForActionAsync(int actionId, int championId,
        bool lockIn = false, string actionType = "pick")
    {
        var body = new Dictionary<string, object>
        {
            ["championId"] = championId,
            ["type"] = actionType
        };
        if (lockIn) body["completed"] = true;

        var content = new StringContent(
            JsonSerializer.Serialize(body, AppJsonContext.Default.DictionaryStringObject), Encoding.UTF8, "application/json");
        var resp = await PatchAsync($"/lol-champ-select/v1/session/actions/{actionId}", content);
        return resp?.IsSuccessStatusCode == true;
    }

    public async Task<bool> CompleteActionAsync(int actionId)
    {
        var content = new StringContent("", Encoding.UTF8, "application/json");
        var resp = await PostAsync(
            $"/lol-champ-select/v1/session/actions/{actionId}/complete", content);
        return resp?.IsSuccessStatusCode == true;
    }

    // --- Rune Page Endpoints ---

    public async Task<JsonElement?> GetRunePagesAsync()
    {
        var resp = await GetAsync("/lol-perks/v1/pages");
        if (resp?.IsSuccessStatusCode == true)
        {
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize(json, AppJsonContext.Default.JsonElement);
        }
        return null;
    }

    public async Task<bool> SetCurrentRunePageAsync(int pageId)
    {
        var content = new StringContent(
            pageId.ToString(), Encoding.UTF8, "application/json");
        var resp = await RequestAsync(HttpMethod.Put, "/lol-perks/v1/currentpage", content);
        return resp?.IsSuccessStatusCode == true;
    }

    [GeneratedRegex(@"--install-directory[=\s]+""?([^""]+)""?")]
    private static partial Regex InstallDirRegex();

    [GeneratedRegex(@"--app-port=(\d+)")]
    private static partial Regex AppPortRegex();

    [GeneratedRegex(@"--remoting-auth-token=([^\s""]+)")]
    private static partial Regex AuthTokenRegex();
}
