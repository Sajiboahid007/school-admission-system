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

    public async Task<Hold> GetAsync(Guid holdId) =>
        await _db.Holds.AsNoTracking().Include(h => h.Class).FirstOrDefaultAsync(h => h.Id == holdId)
        ?? throw new AppException(404, "HOLD_NOT_FOUND", "Application not found.");

    public async Task<Hold> ConfirmAsync(Guid holdId, string studentId)
    {
        var hold = await GetAsync(holdId);

        // R4: only your own application
        if (hold.StudentId != studentId)
            throw new AppException(403, "NOT_YOUR_HOLD", "This application belongs to another student.");

        var now = _clock.UtcNow;

        var updated = await _db.Holds
            .Where(h => h.Id == holdId && h.Status == HoldStatus.Held && h.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Status, HoldStatus.Confirmed)
                .SetProperty(h => h.ConfirmedAt, now));

        if (updated == 1)
        {
            hold.Status = HoldStatus.Confirmed;
            hold.ConfirmedAt = now;
            return hold;
        }

        // Nothing changed: find out why.
        var current = await GetAsync(holdId);

        return HoldMapper.EffectiveStatus(current, now) switch
        {
            HoldStatus.Confirmed => current,
            HoldStatus.Expired => throw new AppException(409, "HOLD_EXPIRED", "The time for this application is over."),
            HoldStatus.Cancelled => throw new AppException(409, "HOLD_CANCELLED", "This application was cancelled."),
            _ => throw new AppException(409, "HOLD_NOT_CONFIRMABLE", "This application cannot be confirmed.")
        };
    }
}
