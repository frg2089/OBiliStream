namespace OBiliStream.Model;

public sealed record class SendChatRequest(int RoomId, string Message);
