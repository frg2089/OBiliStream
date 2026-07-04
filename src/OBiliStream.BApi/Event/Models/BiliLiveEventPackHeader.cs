using System.Diagnostics;
using System.Net;
using System.Text;

namespace OBiliStream.BApi.Event.Models;

public record class BiliLiveEventPackHeader(
    int PacketLength,
    short HeaderLength,
    BiliLiveEventPackBodyType BodyType,
    BiliLiveEventOperation Operation,
    int SequenceId)
{
    public const int Size = 16;
    public BiliLiveEventPackHeader(int packetLength, BiliLiveEventPackBodyType protocolVersion, BiliLiveEventOperation operation)
        : this(packetLength, Size, protocolVersion, operation, 1)
    {
    }

    public void WriteTo(Span<byte> span)
        => Write(PacketLength, HeaderLength, BodyType, Operation, SequenceId, span);

    public static void Write(
        int packetLength,
        short headerLength,
        BiliLiveEventPackBodyType bodyType,
        BiliLiveEventOperation operation,
        int sequenceId,
        Span<byte> span)
    {
        Debug.Assert(span.Length >= Size);

        BitConverter.GetBytes(IPAddress.HostToNetworkOrder(packetLength)).CopyTo(span[0..4]);
        BitConverter.GetBytes(IPAddress.HostToNetworkOrder(headerLength)).CopyTo(span[4..6]);
        BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)bodyType)).CopyTo(span[6..8]);
        BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)operation)).CopyTo(span[8..12]);
        BitConverter.GetBytes(IPAddress.HostToNetworkOrder(sequenceId)).CopyTo(span[12..16]);
    }

    public static void Write(
        int packetLength,
        BiliLiveEventPackBodyType protocolVersion,
        BiliLiveEventOperation operation,
        Span<byte> span)
        => Write(packetLength, Size, protocolVersion, operation, 1, span);

    public static BiliLiveEventPackHeader Parse(ReadOnlySpan<byte> span) => new(
        IPAddress.NetworkToHostOrder(BitConverter.ToInt32(span[0..4])),
        IPAddress.NetworkToHostOrder(BitConverter.ToInt16(span[4..6])),
        (BiliLiveEventPackBodyType)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(span[6..8])),
        (BiliLiveEventOperation)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(span[8..12])),
        IPAddress.NetworkToHostOrder(BitConverter.ToInt32(span[12..16])));

    public static byte[] Pack(string? payload, BiliLiveEventOperation operation)
    {
        int size = payload is not null ? Encoding.UTF8.GetByteCount(payload) : 0;
        size += Size;
        byte[] payloadData = GC.AllocateUninitializedArray<byte>(size);
        Write(size, BiliLiveEventPackBodyType.HeartbeatOrEnterRoom, operation, payloadData);
        Encoding.UTF8.GetBytes(payload, payloadData.AsSpan(Size));
        return payloadData;
    }
}