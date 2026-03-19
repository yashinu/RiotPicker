using System.Text.Json;
using RiotPicker.Models;

namespace RiotPicker.Services;

public class ValorantAutomation
{
    private static Localization L => Localization.Instance;
    private readonly ValorantConnector _connector;
    private readonly ConfigService _config;
    private readonly Action<string, string> _statusCallback;
    private CancellationTokenSource? _cts;
    private bool _locked;
    private string? _lastMatchId;

    public ValorantAutomation(ValorantConnector connector, ConfigService config,
        Action<string, string>? statusCallback = null)
    {
        _connector = connector;
        _config = config;
        _statusCallback = statusCallback ?? ((_, _) => { });
    }

    public void Start()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();
        _ = PollLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private void UpdateStatus(string text, string color = "gray") =>
        _statusCallback(text, color);

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!_connector.Connected)
                {
                    if (!_connector.IsRunning())
                    {
                        UpdateStatus(L.Get("status_val_waiting"), "yellow");
                        await Task.Delay(5000, ct);
                        continue;
                    }
                    if (!await _connector.TryReconnectAsync())
                    {
                        UpdateStatus(L.Get("status_val_connecting"), "yellow");
                        await Task.Delay(3000, ct);
                        continue;
                    }
                }

                var matchId = await _connector.GetPregameMatchIdAsync();

                if (matchId == null)
                {
                    if (_locked)
                    {
                        _locked = false;
                        _lastMatchId = null;
                    }
                    UpdateStatus(L.Get("status_waiting_match"), "green");
                    await Task.Delay(3000, ct);
                    continue;
                }

                if (matchId != _lastMatchId)
                {
                    _locked = false;
                    _lastMatchId = matchId;
                }

                if (_locked)
                {
                    UpdateStatus(L.Get("status_agent_locked"), "green");
                    await Task.Delay(2000, ct);
                    continue;
                }

                await HandleAgentSelectAsync(matchId);
            }
            catch (TaskCanceledException) { break; }
            catch (Exception ex)
            {
                var msg = ex.Message.Length > 50 ? ex.Message[..50] : ex.Message;
                UpdateStatus($"Hata: {msg}", "red");
            }

            await Task.Delay(1000, ct);
        }
    }

    private async Task HandleAgentSelectAsync(string matchId)
    {
        var matchData = await _connector.GetPregameMatchAsync(matchId);
        if (matchData == null) return;
        var data = matchData.Value;

        // Collect agents already selected by teammates
        var takenAgents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (data.TryGetProperty("AllyTeam", out var allyTeam))
        {
            JsonElement players = default;
            if (allyTeam.ValueKind == JsonValueKind.Object
                && allyTeam.TryGetProperty("Players", out players)
                && players.ValueKind == JsonValueKind.Array)
            {
                foreach (var player in players.EnumerateArray())
                {
                    var charId = player.TryGetProperty("CharacterID", out var c) ? c.GetString() ?? "" : "";
                    var subject = player.TryGetProperty("Subject", out var s) ? s.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(charId) && subject != _connector.Puuid)
                        takenAgents.Add(charId);
                }
            }
        }

        var agentPriority = _config.GetValorantAgents();
        if (agentPriority.Count == 0)
        {
            UpdateStatus(L.Get("status_agent_list_empty"), "yellow");
            return;
        }

        foreach (var agentName in agentPriority)
        {
            if (!AgentData.Agents.TryGetValue(agentName, out var agentId)) continue;
            if (takenAgents.Contains(agentId)) continue;

            if (await _connector.SelectAgentAsync(matchId, agentId))
            {
                await Task.Delay(300);
                if (await _connector.LockAgentAsync(matchId, agentId))
                {
                    _locked = true;
                    UpdateStatus(L.Get("status_agent_locked_name", agentName), "green");
                    return;
                }
            }
        }

        UpdateStatus(L.Get("status_no_agent"), "red");
    }
}
