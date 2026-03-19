using Avalonia.Controls;
using RiotPicker.Models;
using RiotPicker.Services;
using RiotPicker.ViewModels;

namespace RiotPicker.Views;

public partial class LolTabView : UserControl
{
    private static Localization L => Localization.Instance;

    public LolTabView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        L.PropertyChanged += (_, _) => ApplyLocalization();
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PickList.AvailableItems = ChampionData.ChampionNames;
        BanList.AvailableItems = ChampionData.ChampionNames;
        ApplyLocalization();

        if (DataContext is LolViewModel vm)
        {
            PickList.ItemsChanged += vm.SavePicks;
            BanList.ItemsChanged += vm.SaveBans;
        }
    }

    private void ApplyLocalization()
    {
        AutoAcceptLabel.Text = L.AutoAccept;
        AutoRuneLabel.Text = L.AutoRune;
        PickTab.Header = L.TabPick;
        BanTab.Header = L.TabBan;
        RoleLabel.Text = L.RoleLabel;
        BanInfoText.Text = L.BanInfo;
        PickList.Placeholder = L.SearchChampion;
        BanList.Placeholder = L.SearchBan;
    }
}
