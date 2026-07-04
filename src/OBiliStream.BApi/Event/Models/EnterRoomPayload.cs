namespace OBiliStream.BApi.Event.Models;

public sealed record EnterRoomPayload(long Uid, int Roomid, int Protover, string Scene, string Platform, int Type, string? Key);
