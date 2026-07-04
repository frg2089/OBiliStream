using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;

using OBiliStream.Model;

[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(IFormFile))]
[JsonSerializable(typeof(SseItem<JsonElement>))]
[JsonSerializable(typeof(StartLiveRequest))]
[JsonSerializable(typeof(UpdateLiveRequest))]
[JsonSerializable(typeof(SendChatRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
