using System.Text.Json;
using RiotPicker.Models;

namespace RiotPicker.Services;

public class LolAutomation
{
    private static Localization L => Localization.Instance;
    private readonly LcuConnector _connector;
    private readonly ConfigService _config;
    private readonly Action<string, string> _statusCallback;
    private CancellationTokenSource? _cts;
    private bool _banCompleted;
    private bool _pickCompleted;
    private bool _runeSet;
    private int _pickIntentChamp;
    private int _banIntentChampId;
    private string? _lastPhase;

    public LolAutomation(LcuConnector connector, ConfigService config,
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
                    if (!LcuConnector.IsLolClientRunning())
                    {
                        UpdateStatus(L.Get("status_lol_waiting"), "yellow");
                        await Task.Delay(5000, ct);
                        continue;
                    }
                    if (!_connector.TryReconnect())
                    {
                        UpdateStatus(L.Get("status_lol_connecting"), "yellow");
                        await Task.Delay(3000, ct);
                        continue;
                    }
                }

                var phase = await _connector.GetGameflowPhaseAsync();

                if (phase == null)
                {
                    _connector.Connected = false;
                    UpdateStatus(L.Get("status_disconnected"), "red");
                    await Task.Delay(3000, ct);
                    continue;
                }

                if (_lastPhase == "ChampSelect" && phase != "ChampSelect")
                {
                    ResetState();
                }
                _lastPhase = phase;

                int delayMs = phase switch
                {
                    "ChampSelect" => 1000,
                    "ReadyCheck" => 500,
                    "InProgress" => 10000,
                    _ => 3000,
                };

                switch (phase)
                {
                    case "ReadyCheck":
                        await HandleReadyCheckAsync();
                        break;
                    case "ChampSelect":
                        await HandleChampSelectAsync();
                        break;
                    case "InProgress":
                        UpdateStatus(L.Get("status_game_in_progress"), "green");
                        break;
                    case "None" or "Lobby" or "Matchmaking":
                        UpdateStatus(L.Get("status_waiting_match"), "green");
                        break;
                    default:
                        UpdateStatus(L.Get("status_phase", phase), "green");
                        break;
                }

                await Task.Delay(delayMs, ct);
            }
            catch (TaskCanceledException) { break; }
            catch
            {
                UpdateStatus(L.Get("status_error_retry"), "red");
                await Task.Delay(3000, ct);
            }
        }
    }

    private async Task TrySetRunePageAsync()
    {
        try
        {
            var champName = _pickIntentChamp > 0
                ? ChampionData.IdToName.GetValueOrDefault(_pickIntentChamp, null)
                : null;
            if (string.IsNullOrEmpty(champName)) return;

            var pagesElement = await _connector.GetRunePagesAsync();
            if (pagesElement == null) return;

            // Search for a page whose name contains the champion name (case-insensitive)
            int matchedPageId = -1;
            string matchedPageName = "";

            foreach (var page in pagesElement.Value.EnumerateArray())
            {
                var pageName = page.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var pageId = page.TryGetProperty("id", out var pid) ? pid.GetInt32() : -1;

                if (pageId > 0 && pageName.Contains(champName, StringComparison.OrdinalIgnoreCase))
                {
                    matchedPageId = pageId;
                    matchedPageName = pageName;
                    break;
                }
            }

            if (matchedPageId > 0)
            {
                await _connector.SetCurrentRunePageAsync(matchedPageId);
                _runeSet = true;
                UpdateStatus(L.Get("status_rune_set", matchedPageName), "green");
            }
            // If no match found, do nothing - keep current rune page
        }
        catch { }
    }

    private void ResetState()
    {
        _banCompleted = false;
        _pickCompleted = false;
        _runeSet = false;
        _pickIntentChamp = 0;
        _banIntentChampId = 0;
    }

    private async Task HandleReadyCheckAsync()
    {
        if (_config.GetAutoAccept())
        {
            await _connector.AcceptMatchAsync();
            UpdateStatus(L.Get("status_match_accepted"), "green");
        }
        else
        {
            UpdateStatus(L.Get("status_match_found_no_auto"), "yellow");
        }
    }

    private List<string> GetPickPriority(string? role)
    {
        List<string> list = [];
        if (!string.IsNullOrEmpty(role))
            list = _config.GetLolPicks(role);
        if (list.Count == 0)
        {
            foreach (var r in RoleData.Roles)
            {
                list = _config.GetLolPicks(r);
                if (list.Count > 0) break;
            }
        }
        return list;
    }

    private static HashSet<int> GetBannedIds(JsonElement session)
    {
        var banned = new HashSet<int>();
        if (session.TryGetProperty("actions", out var actions))
        {
            foreach (var group in actions.EnumerateArray())
            {
                foreach (var action in group.EnumerateArray())
                {
                    if (action.GetProperty("type").GetString() == "ban"
                        && action.GetProperty("completed").GetBoolean()
                        && action.TryGetProperty("championId", out var cid)
                        && cid.GetInt32() > 0)
                    {
                        banned.Add(cid.GetInt32());
                    }
                }
            }
        }
        return banned;
    }

    private (string name, int id)? FindBestBan(JsonElement session)
    {
        var banList = _config.GetBanChampions();
        if (banList.Count == 0) return null;

        var alreadyBanned = GetBannedIds(session);

        var teammateIntents = new HashSet<int>();
        if (session.TryGetProperty("myTeam", out var team))
        {
            foreach (var player in team.EnumerateArray())
            {
                if (player.TryGetProperty("championPickIntent", out var intent) && intent.GetInt32() != 0)
                    teammateIntents.Add(intent.GetInt32());
            }
        }

        var excluded = new HashSet<int>(alreadyBanned);
        excluded.UnionWith(teammateIntents);

        foreach (var champName in banList)
        {
            if (!ChampionData.Champions.TryGetValue(champName, out var champId)) continue;
            if (excluded.Contains(champId)) continue;
            return (champName, champId);
        }
        return null;
    }

    private (string name, int id)? FindBestPick(string? role, int myCellId,
        JsonElement session, HashSet<int>? pickable = null)
    {
        var priorityList = GetPickPriority(role);
        if (priorityList.Count == 0) return null;

        var banned = GetBannedIds(session);

        var taken = new HashSet<int>();
        if (session.TryGetProperty("myTeam", out var team))
        {
            foreach (var player in team.EnumerateArray())
            {
                var cid = player.TryGetProperty("championId", out var c) ? c.GetInt32() : 0;
                var cell = player.TryGetProperty("cellId", out var cl) ? cl.GetInt32() : -1;
                if (cid != 0 && cell != myCellId)
                    taken.Add(cid);
            }
        }

        foreach (var champName in priorityList)
        {
            if (!ChampionData.Champions.TryGetValue(champName, out var champId)) continue;
            if (banned.Contains(champId)) continue;
            if (pickable != null && pickable.Count > 0 && !pickable.Contains(champId)) continue;
            if (taken.Contains(champId)) continue;
            return (champName, champId);
        }
        return null;
    }

    private async Task<bool> CheckActionCompleted(int actionId)
    {
        var session = await _connector.GetChampSelectSessionAsync();
        if (session == null) return false;
        var s = session.Value;
        if (s.TryGetProperty("actions", out var actions))
        {
            foreach (var group in actions.EnumerateArray())
            {
                foreach (var action in group.EnumerateArray())
                {
                    if (action.GetProperty("id").GetInt32() == actionId
                        && action.GetProperty("completed").GetBoolean())
                        return true;
                }
            }
        }
        return false;
    }

    private async Task TryCompleteAction(int actionId, int champId, string actionType, Action onSuccess)
    {
        // Strategy 1: POST /complete
        await _connector.CompleteActionAsync(actionId);
        await Task.Delay(200);
        if (await CheckActionCompleted(actionId)) { onSuccess(); return; }

        // Strategy 2: PATCH with completed=true
        await _connector.SetChampionForActionAsync(actionId, champId, true, actionType);
        await Task.Delay(200);
        if (await CheckActionCompleted(actionId)) { onSuccess(); return; }

        // Strategy 3: PATCH champion without lock
        await _connector.SetChampionForActionAsync(actionId, champId, false, actionType);
    }

    private async Task HandleChampSelectAsync()
    {
        var sessionNullable = await _connector.GetChampSelectSessionAsync();
        if (sessionNullable == null) return;
        var session = sessionNullable.Value;

        if (!session.TryGetProperty("localPlayerCellId", out var cellIdElem)) return;
        var myCellId = cellIdElem.GetInt32();

        // Determine assigned role
        string? myRole = null;
        if (session.TryGetProperty("myTeam", out var myTeam))
        {
            foreach (var player in myTeam.EnumerateArray())
            {
                if (player.TryGetProperty("cellId", out var cell) && cell.GetInt32() == myCellId)
                {
                    myRole = player.TryGetProperty("assignedPosition", out var pos)
                        ? pos.GetString()?.ToUpperInvariant() : null;
                    break;
                }
            }
        }

        // Collect my actions
        var myActions = new List<JsonElement>();
        if (session.TryGetProperty("actions", out var actions))
        {
            foreach (var group in actions.EnumerateArray())
            {
                foreach (var action in group.EnumerateArray())
                {
                    if (action.TryGetProperty("actorCellId", out var actor)
                        && actor.GetInt32() == myCellId)
                    {
                        myActions.Add(action);
                    }
                }
            }
        }

        HashSet<int>? pickableCache = null;
        bool activeHandled = false;

        foreach (var action in myActions)
        {
            var isActive = action.TryGetProperty("isInProgress", out var ip) && ip.GetBoolean();
            var isCompleted = action.TryGetProperty("completed", out var comp) && comp.GetBoolean();
            var actionType = action.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
            var actionId = action.GetProperty("id").GetInt32();
            var champOnAction = action.TryGetProperty("championId", out var ca) ? ca.GetInt32() : 0;

            if (isCompleted) continue;

            // === DECLARING PHASE ===
            if (!isActive)
            {
                if (actionType == "ban" && !_banCompleted)
                {
                    var completedBans = GetBannedIds(session);
                    if (_banIntentChampId != 0 && completedBans.Contains(_banIntentChampId))
                        _banIntentChampId = 0;

                    if (_banIntentChampId == 0)
                    {
                        var bestBan = FindBestBan(session);
                        if (bestBan != null)
                        {
                            var (banName, banId) = bestBan.Value;
                            _banIntentChampId = banId;
                            await _connector.SetChampionForActionAsync(actionId, banId, false, "ban");
                            // ban intent set
                            UpdateStatus(L.Get("status_ban_preparing", banName), "yellow");
                        }
                    }
                    else
                    {
                        var banName = ChampionData.IdToName.GetValueOrDefault(_banIntentChampId, "?");
                        if (champOnAction != _banIntentChampId)
                            await _connector.SetChampionForActionAsync(actionId, _banIntentChampId, false, "ban");
                        UpdateStatus(L.Get("status_ban_preparing", banName), "yellow");
                    }
                }
                else if (actionType == "pick" && !_pickCompleted)
                {
                    pickableCache ??= (await _connector.GetPickableChampionsAsync()).ToHashSet();
                    var best = FindBestPick(myRole, myCellId, session, pickableCache);
                    if (best != null)
                    {
                        var (bestName, bestId) = best.Value;
                        if (champOnAction != bestId)
                            await _connector.SetChampionForActionAsync(actionId, bestId, false, "pick");
                        // pick intent set
                        _pickIntentChamp = bestId;
                    }
                }
            }
            // === ACTIVE PHASE ===
            else
            {
                if (actionType == "ban" && !_banCompleted)
                {
                    activeHandled = true;

                    var timer = session.TryGetProperty("timer", out var timerElem) ? timerElem : default;
                    var timeLeftMs = timer.ValueKind != JsonValueKind.Undefined
                        && timer.TryGetProperty("adjustedTimeLeftInPhase", out var tlm)
                        ? tlm.GetInt64() : 0;
                    var timeLeftS = timeLeftMs / 1000.0;

                    // Check if teammates in same group completed
                    bool teammatesDone = true;
                    if (session.TryGetProperty("actions", out var allActions))
                    {
                        foreach (var group in allActions.EnumerateArray())
                        {
                            bool found = false;
                            foreach (var a in group.EnumerateArray())
                            {
                                if (a.GetProperty("id").GetInt32() == actionId) { found = true; break; }
                            }
                            if (found)
                            {
                                foreach (var a in group.EnumerateArray())
                                {
                                    if (a.TryGetProperty("actorCellId", out var ac) && ac.GetInt32() != myCellId
                                        && a.TryGetProperty("type", out var at) && at.GetString() == "ban"
                                        && !(a.TryGetProperty("completed", out var ac2) && ac2.GetBoolean()))
                                    {
                                        teammatesDone = false;
                                    }
                                }
                                break;
                            }
                        }
                    }

                    var bestBan = FindBestBan(session);
                    bool shouldComplete = teammatesDone || timeLeftS <= 5;

                    if (bestBan != null)
                    {
                        var (banName, banId) = bestBan.Value;
                        _banIntentChampId = banId;

                        if (champOnAction != banId)
                            await _connector.SetChampionForActionAsync(actionId, banId, false, "ban");

                        if (shouldComplete)
                        {
                            var name = banName;
                            await TryCompleteAction(actionId, banId, "ban", () =>
                            {
                                _banCompleted = true;
                                UpdateStatus(L.Get("status_banned", name), "green");
                            });
                        }

                        if (!_banCompleted)
                            UpdateStatus(L.Get("status_ban_countdown", banName, $"{timeLeftS:F0}"), "yellow");
                    }
                }
                else if (actionType == "pick" && !_pickCompleted)
                {
                    activeHandled = true;

                    pickableCache ??= (await _connector.GetPickableChampionsAsync()).ToHashSet();
                    var best = FindBestPick(myRole, myCellId, session, pickableCache);
                    if (best == null)
                    {
                        UpdateStatus(L.Get("status_no_champion"), "red");
                        continue;
                    }

                    var (targetName, targetId) = best.Value;
                    var tName = targetName;
                    await TryCompleteAction(actionId, targetId, "pick", () =>
                    {
                        _pickCompleted = true;
                        UpdateStatus(L.Get("status_picked_locked", tName), "green");
                    });

                    if (!_pickCompleted)
                        UpdateStatus(L.Get("status_pick_locking", targetName), "yellow");
                }
            }
        }

        // Auto rune selection after pick is locked
        if (_pickCompleted && !_runeSet && _config.GetAutoRune())
        {
            await TrySetRunePageAsync();
        }

        // Status update
        if (!activeHandled && !_pickCompleted)
        {
            if (_banCompleted)
            {
                UpdateStatus(L.Get("status_ban_done_waiting"), "green");
            }
            else if (!string.IsNullOrEmpty(myRole))
            {
                var display = RoleData.RoleDisplay.GetValueOrDefault(myRole, myRole);
                UpdateStatus(L.Get("status_champ_select_role", display), "green");
            }
            else
            {
                UpdateStatus(L.Get("status_champ_select"), "green");
            }
        }
    }
}
