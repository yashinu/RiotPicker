using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RiotPicker.Services;
using RiotPicker.ViewModels;

namespace RiotPicker;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        UpdateLangButton();
        Localization.Instance.PropertyChanged += (_, _) => UpdateLangButton();
    }

    private void UpdateLangButton()
    {
        LangButton.Content = Localization.Instance.Lang.ToUpperInvariant();
    }

    private void Language_Click(object? sender, RoutedEventArgs e)
    {
        _vm.ToggleLanguage();
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void Minimize_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        _vm.Shutdown();
        Close();
    }
}
