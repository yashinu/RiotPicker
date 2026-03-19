using CommunityToolkit.Mvvm.ComponentModel;

namespace RiotPicker.Services;

public partial class Localization : ObservableObject
{
    public static Localization Instance { get; } = new();

    private string _lang = "tr";

    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        ["tr"] = new()
        {
            // Status
            ["status_waiting_connection"] = "Baglanti bekleniyor...",
            ["status_disabled"] = "Devre disi",
            ["status_lol_waiting"] = "LoL istemcisi bekleniyor...",
            ["status_lol_connecting"] = "LoL istemcisine baglaniliyor...",
            ["status_disconnected"] = "Baglanti kesildi",
            ["status_waiting_match"] = "Bagli - mac bekleniyor",
            ["status_match_accepted"] = "Mac kabul edildi!",
            ["status_match_found_no_auto"] = "Mac bulundu (otomatik kabul kapali)",
            ["status_game_in_progress"] = "Oyun devam ediyor",
            ["status_error_retry"] = "Hata olustu, tekrar deneniyor...",
            ["status_champ_select"] = "Sampiyon secimi",
            ["status_champ_select_role"] = "Sampiyon secimi ({0})",
            ["status_ban_preparing"] = "{0} yasaklamaya hazirlaniyor",
            ["status_ban_countdown"] = "{0} yasaklanacak ({1}s)",
            ["status_banned"] = "{0} yasaklandi!",
            ["status_ban_done_waiting"] = "Ban tamamlandi, secim sirasi bekleniyor...",
            ["status_picked_locked"] = "{0} secildi ve kilitlendi!",
            ["status_pick_locking"] = "{0} kilitlenmeye calisiyor...",
            ["status_no_champion"] = "Listeden musait sampiyon yok!",
            ["status_phase"] = "Durum: {0}",
            // Valorant
            ["status_val_waiting"] = "Valorant istemcisi bekleniyor...",
            ["status_val_connecting"] = "Valorant istemcisine baglaniliyor...",
            ["status_agent_locked"] = "Ajan kilitlendi",
            ["status_agent_locked_name"] = "{0} kilitlendi!",
            ["status_agent_list_empty"] = "Ajan oncelik listesi bos!",
            ["status_no_agent"] = "Listeden musait ajan yok!",
            // UI
            ["auto_accept"] = "Otomatik Mac Kabul",
            ["tab_pick"] = "Secim",
            ["tab_ban"] = "Yasaklama",
            ["role_label"] = "Koridor:",
            ["ban_info"] = "Ilk musait sampiyon otomatik yasaklanir.\nBiri zaten yasaklanmissa siradakine gecer.",
            ["search_champion"] = "Sampiyon ara...",
            ["search_ban"] = "Yasaklanacak sampiyon ara...",
            ["search_agent"] = "Ajan ara...",
            ["agent_info"] = "Listedeki ilk musait ajan otomatik secilir ve kilitlenir.",
            ["agent_warning"] = "\u26A0  Otomatik ajan kilitleme takim arkadaslariniz tarafindan rapor edilmenize yol acabilir.",
            ["list_empty"] = "Liste bos - asagidan ekleyin",
            ["search_placeholder"] = "Ara...",
            ["language"] = "Dil",
            ["auto_rune"] = "Otomatik Run",
            ["status_rune_set"] = "{0} run sayfasi secildi",
        },
        ["en"] = new()
        {
            // Status
            ["status_waiting_connection"] = "Waiting for connection...",
            ["status_disabled"] = "Disabled",
            ["status_lol_waiting"] = "Waiting for LoL client...",
            ["status_lol_connecting"] = "Connecting to LoL client...",
            ["status_disconnected"] = "Disconnected",
            ["status_waiting_match"] = "Connected - waiting for match",
            ["status_match_accepted"] = "Match accepted!",
            ["status_match_found_no_auto"] = "Match found (auto-accept off)",
            ["status_game_in_progress"] = "Game in progress",
            ["status_error_retry"] = "Error occurred, retrying...",
            ["status_champ_select"] = "Champion select",
            ["status_champ_select_role"] = "Champion select ({0})",
            ["status_ban_preparing"] = "Preparing to ban {0}",
            ["status_ban_countdown"] = "Banning {0} ({1}s)",
            ["status_banned"] = "{0} banned!",
            ["status_ban_done_waiting"] = "Ban done, waiting for pick turn...",
            ["status_picked_locked"] = "{0} picked and locked!",
            ["status_pick_locking"] = "Locking {0}...",
            ["status_no_champion"] = "No available champion from list!",
            ["status_phase"] = "Status: {0}",
            // Valorant
            ["status_val_waiting"] = "Waiting for Valorant client...",
            ["status_val_connecting"] = "Connecting to Valorant client...",
            ["status_agent_locked"] = "Agent locked",
            ["status_agent_locked_name"] = "{0} locked!",
            ["status_agent_list_empty"] = "Agent priority list empty!",
            ["status_no_agent"] = "No available agent from list!",
            // UI
            ["auto_accept"] = "Auto Accept Match",
            ["tab_pick"] = "Pick",
            ["tab_ban"] = "Ban",
            ["role_label"] = "Lane:",
            ["ban_info"] = "First available champion will be auto-banned.\nIf one is already banned, moves to the next.",
            ["search_champion"] = "Search champion...",
            ["search_ban"] = "Search champion to ban...",
            ["search_agent"] = "Search agent...",
            ["agent_info"] = "First available agent from the list will be auto-selected and locked.",
            ["agent_warning"] = "\u26A0  Auto-locking agents may cause your teammates to report you.",
            ["list_empty"] = "List empty - add from below",
            ["search_placeholder"] = "Search...",
            ["language"] = "Language",
            ["auto_rune"] = "Auto Rune",
            ["status_rune_set"] = "{0} rune page selected",
        },
    };

    public string Lang
    {
        get => _lang;
        set
        {
            if (_lang != value && Strings.ContainsKey(value))
            {
                _lang = value;
                OnPropertyChanged(nameof(Lang));
                // Notify all string properties changed
                OnPropertyChanged("");
            }
        }
    }

    public string Get(string key) =>
        Strings.TryGetValue(_lang, out var dict) && dict.TryGetValue(key, out var val) ? val : key;

    public string Get(string key, params object[] args) =>
        string.Format(Get(key), args);

    // Bindable properties for UI
    public string AutoAccept => Get("auto_accept");
    public string TabPick => Get("tab_pick");
    public string TabBan => Get("tab_ban");
    public string RoleLabel => Get("role_label");
    public string BanInfo => Get("ban_info");
    public string SearchChampion => Get("search_champion");
    public string SearchBan => Get("search_ban");
    public string SearchAgent => Get("search_agent");
    public string AgentInfo => Get("agent_info");
    public string AgentWarning => Get("agent_warning");
    public string ListEmpty => Get("list_empty");
    public string StatusDisabled => Get("status_disabled");
    public string StatusWaiting => Get("status_waiting_connection");
    public string LanguageLabel => Get("language");
    public string AutoRune => Get("auto_rune");
}
