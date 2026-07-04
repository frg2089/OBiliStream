using System.Net.WebSockets;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

using OBiliStream.BApi.Danmaku;
using OBiliStream.BApi.Event.Extensions;
using OBiliStream.BApi.Event.Models;
using OBiliStream.BApi.Models;

namespace OBiliStream.BApi.Event;

public sealed class BiliLiveWebSocketEventClient(
    int roomId, long userId, string? token,
    LiveDanmakuServerInfo server,
    BiliApiClient apiClient,
    ILogger<BiliLiveWebSocketEventClient> logger)
    : BiliLiveEventClient(logger)
{
    private ClientWebSocket? _client;
    private Task? _receiving;
    private Task? _heartbeat;
    private bool _disposedValue;

    public override async IAsyncEnumerable<BiliLiveEventPacket> ConnectAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _client = new()
        {
            Options =
            {
               CollectHttpResponseDetails = true,
               KeepAliveInterval = TimeSpan.FromSeconds(60),
               KeepAliveTimeout = TimeSpan.FromSeconds(60),
            },
        };

        await _client.ConnectAsync(server.WSSUri, apiClient.Client, cancellationToken);

        _receiving = ReceivingAsync(cancellationToken);

        await foreach (var packet in base.ConnectAsync(cancellationToken))
            yield return packet;

        if (_heartbeat is not null)
            await _heartbeat;
        await _receiving;
    }

    protected override async Task EnterRoomAsync(CancellationToken cancellationToken = default)
    {
        if (_client is null)
            throw new InvalidOperationException();

        LogEnterRoom(roomId, userId);
        await _client.SendJsonDataAsync(
            new EnterRoomPayload(
                userId,
                roomId,
                3,
                "room",
                "web",
                2,
                token
            ),
            BiliLiveEventOperation.EnterRoom,
            OBiliStreamJsonSerializerContext.Default.EnterRoomPayload,
            cancellationToken);

        _heartbeat = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                logger.LogTrace("发送心跳包");
                await _client.SendJsonDataAsync(BiliLiveEventOperation.Heartbeat, cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }, cancellationToken);
    }

    private async Task ReceivingAsync(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(_client);

        await using MemoryStream ms = new(4096);
        byte[] buffer = new byte[16];
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_client.State is not WebSocketState.Open and not WebSocketState.CloseSent)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }
                var result = await _client.ReceiveAsync(buffer, cancellationToken);
                await ms.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
                if (result.EndOfMessage)
                {
                    await ms.FlushAsync(cancellationToken);
                    ms.Seek(0, SeekOrigin.Begin);
                    byte[] data = ms.ToArray();
                    await ReceivedPackAsync(BiliLiveEventPackHeader.Parse(data), data.AsMemory(16));
                    ms.SetLength(0);
                }
            }
            catch (WebSocketException e) when (e.WebSocketErrorCode is WebSocketError.ConnectionClosedPrematurely)
            {
                logger.LogCritical(e, "Critical");
                throw;
            }
            catch (OperationCanceledException e)
            {
                logger.LogInformation(e, "已退出");
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error");
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposedValue)
            return;
        base.Dispose(disposing);
        if (disposing)
        {
            _client?.Dispose();
        }

        _disposedValue = true;
    }
}
