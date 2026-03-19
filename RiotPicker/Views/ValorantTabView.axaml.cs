using Avalonia.Controls;
using RiotPicker.Models;
using RiotPicker.Services;
using RiotPicker.ViewModels;

namespace RiotPicker.Views;

public partial class ValorantTabView : UserControl
{
    private static Localization L => Localization.Instance;

    public ValorantTabView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        L.PropertyChanged += (_, _) => ApplyLocalization();
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        AgentList.AvailableItems = AgentData.AgentNames;
        ApplyLocalization();

        if (DataContext is ValorantViewModel vm)
            AgentList.ItemsChanged += vm.SaveAgents;
    }

    private void ApplyLocalization()
    {
        InfoText.Text = L.AgentInfo;
        WarningText.Text = L.AgentWarning;
        AgentList.Placeholder = L.SearchAgent;
    }
}
