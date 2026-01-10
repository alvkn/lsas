using System.Text.Json.Serialization;
using LightSteamAccountSwitcher.Core.Models;

namespace LightSteamAccountSwitcher.Core.Utils;

[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(List<SteamProfile>))]
public partial class AppJsonContext : JsonSerializerContext;