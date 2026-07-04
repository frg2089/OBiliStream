using System.Text.Json.Serialization;

using OBiliStream.Model;

[JsonSerializable(typeof(StartLiveRequest))]
[JsonSerializable(typeof(UpdateLiveRequest))]
[JsonSerializable(typeof(SendChatRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
