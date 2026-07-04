using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using OBiliStream.BApi.Event.Models;

namespace OBiliStream.BApi.Event.Extensions;

internal static class WebSocketExtensions
{
    public static async Task SendJsonDataAsync(
        this WebSocket ws,
        string? payload,
        BiliLiveEventOperation operation,
        CancellationToken cancellationToken = default)
        => await ws.SendAsync(
            BiliLiveEventPackHeader.Pack(payload, operation),
            WebSocketMessageType.Binary,
            WebSocketMessageFlags.EndOfMessage,
            cancellationToken);

    public static Task SendJsonDataAsync(
        this WebSocket ws,
        BiliLiveEventOperation operation,
        CancellationToken cancellationToken = default)
        => ws.SendJsonDataAsync("{}", operation, cancellationToken);

    public static Task SendJsonDataAsync<T>(
        this WebSocket ws,
        T data,
        BiliLiveEventOperation operation,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default)
        => ws.SendJsonDataAsync(JsonSerializer.Serialize(data, jsonTypeInfo), operation, cancellationToken);
}