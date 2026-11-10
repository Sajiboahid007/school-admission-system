using AdmissionApi.Common;
using AdmissionApi.Data;
using AdmissionApi.Dtos;
using AdmissionApi.Models;
using AdmissionApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AdmissionApi.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly AdmissionDbContext _db;
    private readonly IClock _clock;
    private readonly HoldService _holds;

    public ClassesController(AdmissionDbContext db, IClock clock, HoldService holds)
    {
        _db = db;
        _clock = clock;
        _holds = holds;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request)
    {
        var name = request.Name!.Trim();
        if (name.Length == 0)
            throw new AppException(400, "VALIDATION_ERROR", "Class name is required.");

        if (await _db.Classes.AnyAsync(c => c.Name == name))
            throw new AppException(409, "CLASS_EXISTS", "A class with this name already exists.");

        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            Name = name,
            TotalSeats = request.TotalSeats!.Value,
            CreatedAt = _clock.UtcNow
        };

        _db.Classes.Add(cls);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new AppException(409, "CLASS_EXISTS", "A class with this name already exists.");
        }

        var body = new ClassResponse(cls.Id.ToString(), cls.Name, cls.TotalSeats, 0, 0, cls.TotalSeats);
        return Created($"/classes/{cls.Id}", body);
    }

    [HttpGet]
    public Task<List<ClassResponse>> List() => QuerySummariesAsync(_db.Classes);

    [HttpGet("{classId}")]
    public async Task<ClassResponse> Get(string classId)
    {
        var id = ParseClassId(classId);
        var result = await QuerySummariesAsync(_db.Classes.Where(c => c.Id == id));
        return result.FirstOrDefault()
            ?? throw new AppException(404, "CLASS_NOT_FOUND", "Class not found.");
    }

    [HttpPost("{classId}/holds")]
    public async Task<IActionResult> CreateHold(string classId)
    {
        var studentId = StudentIdHeader.Require(Request);
        var hold = await _holds.CreateHoldAsync(ParseClassId(classId), studentId);
        return StatusCode(StatusCodes.Status201Created, HoldMapper.ToCreated(hold, _clock.UtcNow));
    }

    private static Guid ParseClassId(string raw) =>
        Guid.TryParse(raw, out var id)
            ? id
            : throw new AppException(404, "CLASS_NOT_FOUND", "Class not found.");

    private async Task<List<ClassResponse>> QuerySummariesAsync(IQueryable<SchoolClass> query)
    {
        var now = _clock.UtcNow;

        var rows = await query
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                Held = c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                Confirmed = c.Holds.Count(h => h.Status == HoldStatus.Confirmed)
            })
            .ToListAsync();

        return rows.Select(r => new ClassResponse(
                r.Id.ToString(), r.Name, r.TotalSeats, r.Held, r.Confirmed,
                Math.Max(0, r.TotalSeats - r.Held - r.Confirmed)))
            .ToList();
    }
}
