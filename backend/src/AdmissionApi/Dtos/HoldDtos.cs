namespace AdmissionApi.Dtos;

public record HoldResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    string Status,
    DateTime ExpiresAt,
    DateTime ServerTime);

public record CreatedHoldResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    string StudentId,
    string Status,
    DateTime ExpiresAt,
    DateTime ServerTime);
