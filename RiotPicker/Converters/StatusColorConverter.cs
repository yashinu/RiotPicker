using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RiotPicker.Converters;

public class StatusColorConverter : IValueConverter
{
    private static readonly Dictionary<string, ISolidColorBrush> Colors = new()
    {
        ["green"] = new SolidColorBrush(Color.Parse("#FF2ECC71")),
        ["yellow"] = new SolidColorBrush(Color.Parse("#FFF39C12")),
        ["red"] = new SolidColorBrush(Color.Parse("#FFE74C3C")),
        ["gray"] = new SolidColorBrush(Color.Parse("#FF95A5A6")),
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && Colors.TryGetValue(s, out var brush) ? brush : Colors["gray"];

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
