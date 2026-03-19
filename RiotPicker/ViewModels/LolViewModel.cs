using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RiotPicker.Models;
using RiotPicker.Services;

namespace RiotPicker.ViewModels;

public partial class LolViewModel : ObservableObject
{
    private readonly ConfigService _config;

    [ObservableProperty] private bool _autoAccept;
    [ObservableProperty] private bool _autoRune;
    [ObservableProperty] private string _currentRole = "TOP";
    [ObservableProperty] private int _selectedRoleIndex;

    public ObservableCollection<string> PickItems { get; } = [];
    public ObservableCollection<string> BanItems { get; } = [];

    public static List<string> ChampionNames => ChampionData.ChampionNames;
    public static string[] RoleDisplayNames => ["TOP", "JGL", "MID", "ADC", "SUP"];

    public LolViewModel(ConfigService config)
    {
        _config = config;
        _autoAccept = config.GetAutoAccept();
        _autoRune = config.GetAutoRune();
        LoadPicksForRole("TOP");
        LoadBans();
    }

    partial void OnAutoAcceptChanged(bool value) => _config.SetAutoAccept(value);
    partial void OnAutoRuneChanged(bool value) => _config.SetAutoRune(value);

    partial void OnSelectedRoleIndexChanged(int value)
    {
        if (value >= 0 && value < RoleDisplayNames.Length)
        {
            var display = RoleDisplayNames[value];
            CurrentRole = RoleData.RoleFromDisplay.GetValueOrDefault(display, display);
            LoadPicksForRole(CurrentRole);
        }
    }

    private void LoadPicksForRole(string role)
    {
        PickItems.Clear();
        foreach (var item in _config.GetLolPicks(role))
            PickItems.Add(item);
    }

    private void LoadBans()
    {
        BanItems.Clear();
        foreach (var item in _config.GetBanChampions())
            BanItems.Add(item);
    }

    public void SavePicks() => _config.SetLolPicks(CurrentRole, [.. PickItems]);
    public void SaveBans() => _config.SetBanChampions([.. BanItems]);
}
