using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

using OBiliStream.BApi.Danmaku;
using OBiliStream.BApi.Event.Extensions;
using OBiliStream.BApi.Event.Models;
using OBiliStream.BApi.Models;

namespace OBiliStream.BApi.Event;

public sealed class BiliLiveTcpEventClient(
    int roomId, long userId, string? token,
    LiveDanmakuServerInfo server,
    ILogger<BiliLiveTcpEventClient> logger)
    : BiliLiveEventClient(logger)
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private Task? _receiving;
    private Task? _heartbeat;
    private bool _disposedValue;

    public override async IAsyncEnumerable<BiliLiveEventPacket> ConnectAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _client = new();

        await _client.ConnectAsync(server.Host, server.Port, cancellationToken);

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

        _stream = _client.GetStream();

        LogEnterRoom(roomId, userId);
        await _stream.SendJsonDataAsync(
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
                await _stream.SendJsonDataAsync(BiliLiveEventOperation.Heartbeat, cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }, cancellationToken);
    }

    private async Task ReceivingAsync(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(_stream);

        await using MemoryStream ms = new(4096);
        byte[] buffer = new byte[16];
        while (true)
        {
            try
            {
                if (!_stream.DataAvailable)
                {
                    if (ms.Length is not 0)
                    {
                        await ms.FlushAsync(cancellationToken);
                        ms.Seek(0, SeekOrigin.Begin);
                        byte[] data = ms.ToArray();
                        await ReceivedPackAsync(BiliLiveEventPackHeader.Parse(data), data.AsMemory(16));
                        ms.SetLength(0);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }
                int result = await _stream.ReadAsync(buffer);
                await ms.WriteAsync(buffer.AsMemory(0, result), cancellationToken);
            }
            catch (WebSocketException e) when (e.WebSocketErrorCode is WebSocketError.ConnectionClosedPrematurely)
            {
                logger.LogCritical(e, "Critical");
                throw;
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