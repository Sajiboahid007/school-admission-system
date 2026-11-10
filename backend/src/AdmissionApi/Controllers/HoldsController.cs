using AdmissionApi.Common;
using AdmissionApi.Dtos;
using AdmissionApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdmissionApi.Controllers;

[ApiController]
public class HoldsController : ControllerBase
{
    private readonly HoldService _holds;
    private readonly IClock _clock;

    public HoldsController(HoldService holds, IClock clock)
    {
        _holds = holds;
        _clock = clock;
    }

    [HttpGet("holds/{holdId}")]
    public async Task<HoldResponse> Get(string holdId)
    {
        var hold = await _holds.GetAsync(ParseHoldId(holdId));
        return HoldMapper.ToResponse(hold, _clock.UtcNow);
    }

    [HttpPost("holds/{holdId}/confirm")]
    public async Task<HoldResponse> Confirm(string holdId)
    {
        var studentId = StudentIdHeader.Require(Request);
        var hold = await _holds.ConfirmAsync(ParseHoldId(holdId), studentId);
        return HoldMapper.ToResponse(hold, _clock.UtcNow);
    }

    private static Guid ParseHoldId(string raw) =>
        Guid.TryParse(raw, out var id)
            ? id
            : throw new AppException(404, "HOLD_NOT_FOUND", "Application not found.");
}
