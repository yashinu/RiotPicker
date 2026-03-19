using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RiotPicker.Models;

namespace RiotPicker.Services;

public class ValorantConnector : BaseConnector
{
    public string? Puuid { get; private set; }
    public string? EntitlementsToken { get; private set; }
    public string? AuthToken { get; private set; }

    public ValorantConnector() : base(GetLockfilePath())
    {
    }

    private static string GetLockfilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return System.IO.Path.Combine(localAppData, "Riot Games", "Riot Client", "Config", "lockfile");
    }

    public async Task<bool> TryReconnectAsync()
    {
        if (!ParseLockfile()) return false;
        return await FetchAuthAsync();
    }

    private async Task<bool> FetchAuthAsync()
    {
        try
        {
            var resp = await GetAsync("/entitlements/v1/token");
            if (resp?.IsSuccessStatusCode != true)
            {
                Connected = false;
                return false;
            }

            var json = await resp.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize(json, AppJsonContext.Default.JsonElement);

            EntitlementsToken = data.GetProperty("token").GetString();
            Puuid = data.GetProperty("subject").GetString();
            AuthToken = data.GetProperty("accessToken").GetString();

            if (string.IsNullOrEmpty(Puuid))
            {
                Connected = false;
                return false;
            }

            Connected = true;
            return true;
        }
        catch
        {
            Connected = false;
            return false;
        }
    }

    private Dictionary<string, string> GlzHeaders()
    {
        var headers = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(EntitlementsToken))
            headers["X-Riot-Entitlements-JWT"] = EntitlementsToken;
        if (!string.IsNullOrEmpty(AuthToken))
            headers["Authorization"] = $"Bearer {AuthToken}";
        return headers;
    }

    public async Task<string?> GetPregameMatchIdAsync()
    {
        var resp = await GetAsync($"/pregame/v1/players/{Puuid}", GlzHeaders());
        if (resp?.IsSuccessStatusCode == true)
        {
            var json = await resp.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize(json, AppJsonContext.Default.JsonElement);
            if (data.TryGetProperty("MatchID", out var matchId))
            {
                var id = matchId.GetString();
                return string.IsNullOrEmpty(id) ? null : id;
            }
        }
        return null;
    }

    public async Task<JsonElement?> GetPregameMatchAsync(string matchId)
    {
        var resp = await GetAsync($"/pregame/v1/matches/{matchId}", GlzHeaders());
        if (resp?.IsSuccessStatusCode == true)
        {
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        return null;
    }

    public async Task<bool> SelectAgentAsync(string matchId, string agentId)
    {
        var resp = await PostAsync(
            $"/pregame/v1/matches/{matchId}/select/{agentId}",
            headers: GlzHeaders());
        return resp?.IsSuccessStatusCode == true;
    }

    public async Task<bool> LockAgentAsync(string matchId, string agentId)
    {
        var resp = await PostAsync(
            $"/pregame/v1/matches/{matchId}/lock/{agentId}",
            headers: GlzHeaders());
        return resp?.IsSuccessStatusCode == true;
    }
}
