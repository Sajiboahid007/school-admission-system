using AdmissionApi.Common;
using AdmissionApi.Data;
using AdmissionApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

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

        var alreadyApplied = await _db.Holds.AnyAsync(h =>
            h.ClassId == classId &&
            h.StudentId == studentId &&
            (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed));

        if (alreadyApplied)
            throw new AppException(409, "ALREADY_APPLIED", "This student already has an active application for this class.");

        var usedSeats = await _db.Holds.CountAsync(h =>
            h.ClassId == classId &&
            (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed));

        if (usedSeats >= cls.TotalSeats)
            throw new AppException(409, "NO_SEATS", "No seats are available in this class.");

        var hold = new Hold
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(_options.DurationSeconds)
        };
        _db.Holds.Add(hold);

        // safety net: the partial unique index
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new AppException(409, "ALREADY_APPLIED", "This student already has an active application for this class.");
        }

        await tx.CommitAsync();

        hold.Class = cls;
        return hold;
    }
}
