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
[JsonSerializable(typeof(BiliApiResult<JsonElement>))]
[JsonSerializable(typeof(BiliPassportQRCodeData))]
[JsonSerializable(typeof(BiliQRCodeLoginStatusData))]
[JsonSerializable(typeof(BuvidData))]
[JsonSerializable(typeof(CookieCollection))]
[JsonSerializable(typeof(EnterRoomPayload))]
[JsonSerializable(typeof(FileContent))]
[JsonSerializable(typeof(HomePageLiveVersion))]
[JsonSerializable(typeof(ImageUploadedData))]
[JsonSerializable(typeof(IReadOnlyList<LiveAreaCategory>))]
[JsonSerializable(typeof(LiveAreaCategory))]
[JsonSerializable(typeof(LiveDanmakuServerData))]
[JsonSerializable(typeof(LiveDanmakuServerInfo))]
[JsonSerializable(typeof(LiveRoomInfoData))]
[JsonSerializable(typeof(LiveRoomInfoDataFromMid))]
[JsonSerializable(typeof(PersonData))]
[JsonSerializable(typeof(QRCodeStatus))]
[JsonSerializable(typeof(RefreshCookieData))]
[JsonSerializable(typeof(StartLiveData))]
public partial class OBiliStreamJsonSerializerContext : JsonSerializerContext
{
}
