using AdmissionApi.Dtos;
using AdmissionApi.Models;

namespace AdmissionApi.Services;

public static class HoldMapper
{
    public static HoldStatus EffectiveStatus(Hold h, DateTime now) =>
        h.Status == HoldStatus.Held && h.ExpiresAt <= now ? HoldStatus.Expired : h.Status;

    public static string ToApi(HoldStatus s) => s.ToString().ToUpperInvariant();

    public static HoldResponse ToResponse(Hold h, DateTime now) => new(
        h.Id.ToString(), h.ClassId.ToString(), h.Class?.Name ?? "",
        ToApi(EffectiveStatus(h, now)), h.ExpiresAt, now);

    public static CreatedHoldResponse ToCreated(Hold h, DateTime now) => new(
        h.Id.ToString(), h.ClassId.ToString(), h.Class?.Name ?? "", h.StudentId,
        ToApi(EffectiveStatus(h, now)), h.ExpiresAt, now);
}
