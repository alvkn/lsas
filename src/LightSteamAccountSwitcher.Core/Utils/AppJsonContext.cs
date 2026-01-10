using System.Text.Json.Serialization;
using LightSteamAccountSwitcher.Core.Models;
using LightSteamAccountSwitcher.Core.Services;

namespace LightSteamAccountSwitcher.Core.Utils;

[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(List<SteamProfile>))]
public partial class AppJsonContext : JsonSerializerContext
{
}
