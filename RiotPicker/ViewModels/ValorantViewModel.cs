using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RiotPicker.Models;
using RiotPicker.Services;

namespace RiotPicker.ViewModels;

public partial class ValorantViewModel : ObservableObject
{
    private readonly ConfigService _config;

    public ObservableCollection<string> AgentItems { get; } = [];
    public static List<string> AgentNames => AgentData.AgentNames;

    public ValorantViewModel(ConfigService config)
    {
        _config = config;
        foreach (var agent in config.GetValorantAgents())
            AgentItems.Add(agent);
    }

    public void SaveAgents() => _config.SetValorantAgents([.. AgentItems]);
}
