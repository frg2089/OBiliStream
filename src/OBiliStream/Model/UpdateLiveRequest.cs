namespace OBiliStream.Model;

public sealed record class UpdateLiveRequest(int RoomId, string? Platform = default, string? VisitId = default, string? Title = default, int? AreaId = default, string? AddTag = default, string? DelTag = default);
