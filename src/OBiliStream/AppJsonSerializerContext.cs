using System.Text.Json.Serialization;

using OBiliStream.BApi.Models;

[JsonSerializable(typeof(StartLiveData))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
