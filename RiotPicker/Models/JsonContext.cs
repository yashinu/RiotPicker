using System.Text.Json;
using System.Text.Json.Serialization;

namespace RiotPicker.Models;

[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class AppJsonContext : JsonSerializerContext
{
}
