using AdmissionApi.Common;
using AdmissionApi.Data;
using AdmissionApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AdmissionApi.Services;

public class HoldService
{
    private readonly AdmissionDbContext _db;
    private readonly IClock _clock;
    private readonly HoldOptions _options;

    public HoldService(AdmissionDbContext db, IClock clock, IOptions<HoldOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<Hold> CreateHoldAsync(Guid classId, string studentId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var cls = (await _db.Classes
                .FromSqlInterpolated($"SELECT * FROM classes WHERE id = {classId} FOR UPDATE")
                .ToListAsync())
            .FirstOrDefault();

        if (cls is null)
            throw new AppException(404, "CLASS_NOT_FOUND", "Class not found.");

        var now = _clock.UtcNow;

        await _db.Holds
            .Where(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired));

        await tx.CommitAsync();
        return new Hold { Class = cls };
    }
}
