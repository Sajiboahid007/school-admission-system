using AdmissionApi.Common;
using AdmissionApi.Data;
using AdmissionApi.Dtos;
using AdmissionApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdmissionApi.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly AdmissionDbContext _db;
    private readonly IClock _clock;

    public ClassesController(AdmissionDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request)
    {
        var name = request.Name!.Trim();
        if (name.Length == 0)
            return BadRequest(new { error = "VALIDATION_ERROR", message = "Class name is required." });

        if (await _db.Classes.AnyAsync(c => c.Name == name))
            return Conflict(new { error = "CLASS_EXISTS", message = "A class with this name already exists." });

        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            Name = name,
            TotalSeats = request.TotalSeats!.Value,
            CreatedAt = _clock.UtcNow
        };

        _db.Classes.Add(cls);
        await _db.SaveChangesAsync();

        var body = new ClassResponse(cls.Id.ToString(), cls.Name, cls.TotalSeats, 0, 0, cls.TotalSeats);
        return Created($"/classes/{cls.Id}", body);
    }
}
