using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using RiotPicker.Services;

namespace RiotPicker.Controls;

public partial class PriorityListControl : UserControl
{
    public static readonly StyledProperty<ObservableCollection<string>?> ItemsSourceProperty =
        AvaloniaProperty.Register<PriorityListControl, ObservableCollection<string>?>(nameof(ItemsSource));

    public static readonly StyledProperty<IList<string>?> AvailableItemsProperty =
        AvaloniaProperty.Register<PriorityListControl, IList<string>?>(nameof(AvailableItems));

    public static readonly StyledProperty<string> PlaceholderProperty =
        AvaloniaProperty.Register<PriorityListControl, string>(nameof(Placeholder), "Ara...");

    public ObservableCollection<string>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IList<string>? AvailableItems
    {
        get => GetValue(AvailableItemsProperty);
        set => SetValue(AvailableItemsProperty, value);
    }

    public string Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public event Action? ItemsChanged;

    private static readonly SolidColorBrush RowBg = new(Color.Parse("#FF252525"));
    private static readonly SolidColorBrush RowHover = new(Color.Parse("#FF303030"));
    private static readonly SolidColorBrush DragTargetBrush = new(Color.Parse("#FF3A3A3A"));
    private static readonly SolidColorBrush AccentBrush = new(Color.Parse("#FFE74C3C"));
    private static readonly SolidColorBrush TextBrush = new(Color.Parse("#FFE0E0E0"));
    private static readonly SolidColorBrush SubBrush = new(Color.Parse("#FF888888"));
    private static readonly SolidColorBrush DragHandleBrush = new(Color.Parse("#FF555555"));
    private static readonly SolidColorBrush PopupItemHover = new(Color.Parse("#FF303030"));

    // Drag state
    private int _dragIndex = -1;
    private bool _isDragging;
    private Point _dragStartPos;
    private Border? _dragGhost;
    private const double DragThreshold = 5;

    public PriorityListControl()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            if (change.OldValue is ObservableCollection<string> old)
                old.CollectionChanged -= OnCollectionChanged;
            if (change.NewValue is ObservableCollection<string> n)
                n.CollectionChanged += OnCollectionChanged;
            RebuildList();
        }
        else if (change.Property == PlaceholderProperty)
        {
            SearchBox.Watermark = Placeholder;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildList();

    private void RebuildList()
    {
        ItemsPanel.Children.Clear();
        var items = ItemsSource;
        if (items == null || items.Count == 0)
        {
            EmptyText.Text = Localization.Instance.ListEmpty;
            EmptyText.IsVisible = true;
            return;
        }
        EmptyText.IsVisible = false;

        for (int i = 0; i < items.Count; i++)
        {
            var idx = i;
            var name = items[i];

            var row = new Border
            {
                Background = RowBg, CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4, 3), Margin = new Thickness(2),
                Tag = idx, Cursor = new Cursor(StandardCursorType.Hand),
            };

            var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("24,24,*,Auto") };

            // Drag handle
            var dragHandle = new TextBlock
            {
                Text = "\u2630", Foreground = DragHandleBrush,
                FontSize = 14, VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.SizeAll),
            };
            Grid.SetColumn(dragHandle, 0);
            grid.Children.Add(dragHandle);

            // Priority number
            var numText = new TextBlock
            {
                Text = $"{i + 1}.", Foreground = AccentBrush,
                FontWeight = FontWeight.Bold, FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            Grid.SetColumn(numText, 1);
            grid.Children.Add(numText);

            // Name
            var nameText = new TextBlock
            {
                Text = name, Foreground = TextBrush, FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0),
            };
            Grid.SetColumn(nameText, 2);
            grid.Children.Add(nameText);

            // Remove button only
            var removeBtn = new Button
            {
                Content = "\u2715", Width = 28, Height = 24,
                FontSize = 10, Foreground = TextBrush,
                Background = AccentBrush, BorderThickness = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(3), Padding = new Thickness(0),
            };
            removeBtn.Click += (_, _) => RemoveItem(idx);
            Grid.SetColumn(removeBtn, 3);
            grid.Children.Add(removeBtn);

            row.Child = grid;

            // Drag events on the row
            row.PointerPressed += Row_PointerPressed;
            row.PointerMoved += Row_PointerMoved;
            row.PointerReleased += Row_PointerReleased;

            // Hover effect
            row.PointerEntered += (_, _) => { if (!_isDragging) row.Background = RowHover; };
            row.PointerExited += (_, _) => { if (!_isDragging) row.Background = RowBg; };

            ItemsPanel.Children.Add(row);
        }
    }

    private void Row_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border row || !e.GetCurrentPoint(row).Properties.IsLeftButtonPressed)
            return;

        // Don't start drag if clicking the remove button
        if (e.Source is Button) return;

        _dragIndex = (int)(row.Tag ?? -1);
        _dragStartPos = e.GetPosition(ItemsPanel);
        _isDragging = false;
        e.Pointer.Capture(row);
    }

    private void Row_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragIndex < 0 || sender is not Border row) return;

        var pos = e.GetPosition(ItemsPanel);

        if (!_isDragging)
        {
            var delta = pos - _dragStartPos;
            if (Math.Abs(delta.Y) < DragThreshold) return;
            _isDragging = true;
            row.Opacity = 0.5;
        }

        // Find which row we're over
        var targetIdx = GetRowIndexAtPosition(pos);
        if (targetIdx >= 0 && targetIdx != _dragIndex && ItemsSource != null)
        {
            // Highlight drop target
            for (int i = 0; i < ItemsPanel.Children.Count; i++)
            {
                if (ItemsPanel.Children[i] is Border b)
                    b.Background = i == targetIdx ? DragTargetBrush : RowBg;
            }
        }
    }

    private void Row_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Border row) return;
        e.Pointer.Capture(null);

        if (_isDragging && _dragIndex >= 0 && ItemsSource != null)
        {
            var pos = e.GetPosition(ItemsPanel);
            var targetIdx = GetRowIndexAtPosition(pos);

            if (targetIdx >= 0 && targetIdx != _dragIndex)
            {
                ItemsSource.Move(_dragIndex, targetIdx);
                ItemsChanged?.Invoke();
            }
            else
            {
                // Reset visual
                row.Opacity = 1;
                RebuildList();
            }
        }

        _dragIndex = -1;
        _isDragging = false;
    }

    private int GetRowIndexAtPosition(Point pos)
    {
        for (int i = 0; i < ItemsPanel.Children.Count; i++)
        {
            if (ItemsPanel.Children[i] is Border child)
            {
                var bounds = child.Bounds;
                if (pos.Y >= bounds.Top && pos.Y <= bounds.Bottom)
                    return i;
            }
        }
        // If below all items, return last index
        if (pos.Y > 0 && ItemsPanel.Children.Count > 0)
            return ItemsPanel.Children.Count - 1;
        return -1;
    }

    private void RemoveItem(int idx)
    {
        if (ItemsSource != null && idx >= 0 && idx < ItemsSource.Count)
        {
            ItemsSource.RemoveAt(idx);
            ItemsChanged?.Invoke();
        }
    }

    private void SearchBox_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TryAddFromSearch();
            return;
        }
        var text = SearchBox.Text?.Trim().ToLowerInvariant() ?? "";
        if (text.Length >= 1)
            ShowResults(text);
        else
            SearchPopup.IsOpen = false;
    }

    private void ShowResults(string searchText)
    {
        SearchResultsPanel.Children.Clear();
        var addable = GetAddableItems();
        var filtered = addable.Where(i => i.ToLowerInvariant().Contains(searchText)).Take(6).ToList();
        if (filtered.Count == 0)
        {
            SearchPopup.IsOpen = false;
            return;
        }

        foreach (var item in filtered)
        {
            var btn = new Button
            {
                Content = item, Background = Brushes.Transparent,
                Foreground = TextBrush, FontSize = 12,
                Padding = new Thickness(8, 5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                BorderThickness = new Thickness(0), CornerRadius = new CornerRadius(3),
            };
            btn.PointerEntered += (_, _) => btn.Background = PopupItemHover;
            btn.PointerExited += (_, _) => btn.Background = Brushes.Transparent;
            btn.Click += (_, _) => SelectFromSearch(item);
            SearchResultsPanel.Children.Add(btn);
        }
        SearchPopup.IsOpen = true;
    }

    private List<string> GetAddableItems()
    {
        var current = new HashSet<string>(ItemsSource ?? []);
        return (AvailableItems ?? []).Where(i => !current.Contains(i)).ToList();
    }

    private void SelectFromSearch(string name)
    {
        SearchBox.Text = "";
        SearchPopup.IsOpen = false;
        AddItem(name);
    }

    private void TryAddFromSearch()
    {
        var text = SearchBox.Text?.Trim() ?? "";
        var addable = GetAddableItems();
        var match = addable.FirstOrDefault(i => i.Equals(text, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            SearchBox.Text = "";
            SearchPopup.IsOpen = false;
            AddItem(match);
        }
    }

    private void AddItem(string value)
    {
        if (ItemsSource != null && !ItemsSource.Contains(value))
        {
            ItemsSource.Add(value);
            ItemsChanged?.Invoke();
        }
    }
}
