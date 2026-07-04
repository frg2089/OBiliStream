using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using OBiliStream.BApi.Event.Models;
using OBiliStream.BApi.Models;

namespace OBiliStream.BApi;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(CookieCollection))]
[JsonSerializable(typeof(BiliApiResult<JsonElement>))]
[JsonSerializable(typeof(PersonData))]
[JsonSerializable(typeof(ImageUploadedData))]
[JsonSerializable(typeof(BuvidData))]
[JsonSerializable(typeof(LiveRoomInfoData))]
[JsonSerializable(typeof(LiveRoomInfoDataFromMid))]
[JsonSerializable(typeof(HomePageLiveVersion))]
[JsonSerializable(typeof(StartLiveData))]
[JsonSerializable(typeof(LiveDanmakuServerData))]
[JsonSerializable(typeof(BiliPassportQRCodeData))]
[JsonSerializable(typeof(BiliQRCodeLoginStatusData))]
[JsonSerializable(typeof(RefreshCookieData))]
[JsonSerializable(typeof(EnterRoomPayload))]
[JsonSerializable(typeof(IReadOnlyList<LiveAreaCategory>))]
public partial class OBiliStreamJsonSerializerContext : JsonSerializerContext
{
}
