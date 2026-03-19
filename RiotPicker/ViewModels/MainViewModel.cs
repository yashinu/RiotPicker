using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using RiotPicker.Services;

namespace RiotPicker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigService _config;
    private static Localization L => Localization.Instance;
    private LcuConnector? _lcuConnector;
    private ValorantConnector? _valConnector;
    private LolAutomation? _lolAuto;
    private ValorantAutomation? _valAuto;

    // Tracks the localization key of the current status so we can re-translate on language change
    private string _lolStatusKey = "status_waiting_connection";
    private object[]? _lolStatusArgs;
    private string _valStatusKey = "status_waiting_connection";
    private object[]? _valStatusArgs;

    [ObservableProperty] private bool _lolEnabled;
    [ObservableProperty] private bool _valEnabled;
    [ObservableProperty] private string _lolStatusText = Localization.Instance.Get("status_waiting_connection");
    [ObservableProperty] private string _lolStatusColor = "gray";
    [ObservableProperty] private string _valStatusText = Localization.Instance.Get("status_waiting_connection");
    [ObservableProperty] private string _valStatusColor = "gray";

    public LolViewModel LolVm { get; }
    public ValorantViewModel ValVm { get; }

    public MainViewModel()
    {
        _config = new ConfigService();
        L.Lang = _config.GetLanguage();

        LolVm = new LolViewModel(_config);
        ValVm = new ValorantViewModel(_config);

        _lolEnabled = _config.GetLolEnabled();
        _valEnabled = _config.GetValEnabled();

        if (_lolEnabled) StartLol();
        else { SetLolStatus("status_disabled", "gray"); }

        if (_valEnabled) StartVal();
        else { SetValStatus("status_disabled", "gray"); }
    }

    public void ToggleLanguage()
    {
        var newLang = L.Lang == "tr" ? "en" : "tr";
        L.Lang = newLang;
        _config.SetLanguage(newLang);

        // Re-translate only static statuses (disabled).
        // Running automation statuses will auto-update on next poll cycle.
        if (!LolEnabled)
            LolStatusText = L.Get("status_disabled");
        if (!ValEnabled)
            ValStatusText = L.Get("status_disabled");
    }

    private void SetLolStatus(string key, string color, params object[] args)
    {
        _lolStatusKey = key;
        _lolStatusArgs = args.Length > 0 ? args : null;
        LolStatusText = args.Length > 0 ? L.Get(key, args) : L.Get(key);
        LolStatusColor = color;
    }

    private void SetValStatus(string key, string color, params object[] args)
    {
        _valStatusKey = key;
        _valStatusArgs = args.Length > 0 ? args : null;
        ValStatusText = args.Length > 0 ? L.Get(key, args) : L.Get(key);
        ValStatusColor = color;
    }

    partial void OnLolEnabledChanged(bool value)
    {
        _config.SetLolEnabled(value);
        if (value) StartLol();
        else { StopLol(); SetLolStatus("status_disabled", "gray"); }
    }

    partial void OnValEnabledChanged(bool value)
    {
        _config.SetValEnabled(value);
        if (value) StartVal();
        else { StopVal(); SetValStatus("status_disabled", "gray"); }
    }

    private void StartLol()
    {
        _lcuConnector ??= new LcuConnector();
        _lolAuto ??= new LolAutomation(_lcuConnector, _config, UpdateLolStatus);
        _lolAuto.Start();
    }

    private void StopLol() => _lolAuto?.Stop();

    private void StartVal()
    {
        _valConnector ??= new ValorantConnector();
        _valAuto ??= new ValorantAutomation(_valConnector, _config, UpdateValStatus);
        _valAuto.Start();
    }

    private void StopVal() => _valAuto?.Stop();

    private void UpdateLolStatus(string text, string color)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Status text comes pre-translated from automation services
            _lolStatusKey = "";
            _lolStatusArgs = null;
            LolStatusText = text;
            LolStatusColor = color;
        });
    }

    private void UpdateValStatus(string text, string color)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _valStatusKey = "";
            _valStatusArgs = null;
            ValStatusText = text;
            ValStatusColor = color;
        });
    }

    public void Shutdown()
    {
        _lolAuto?.Stop();
        _valAuto?.Stop();
    }
}
