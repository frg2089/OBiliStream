using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using OBiliStream.BApi.Event.Models;

namespace OBiliStream.BApi.Event.Extensions;

internal static class StreamExtensions
{
    public static async Task SendJsonDataAsync(
        this Stream stream,
        string? payload,
        BiliLiveEventOperation operation,
        CancellationToken cancellationToken = default)
    {
        await stream.WriteAsync(BiliLiveEventPackHeader.Pack(payload, operation), cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static Task SendJsonDataAsync(
        this Stream stream,
        BiliLiveEventOperation operation,
        CancellationToken cancellationToken = default)
        => stream.SendJsonDataAsync("{}", operation, cancellationToken);

    public static Task SendJsonDataAsync<T>(
        this Stream stream,
        T data,
        BiliLiveEventOperation operation,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default)
        => stream.SendJsonDataAsync(JsonSerializer.Serialize(data, jsonTypeInfo), operation, cancellationToken);
}