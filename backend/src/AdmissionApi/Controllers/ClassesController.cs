using AdmissionApi.Common;
using AdmissionApi.Data;
using AdmissionApi.Dtos;
using AdmissionApi.Models;
using Microsoft.AspNetCore.Mvc;

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
        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            Name = request.Name!.Trim(),
            TotalSeats = request.TotalSeats!.Value,
            CreatedAt = _clock.UtcNow
        };

        _db.Classes.Add(cls);
        await _db.SaveChangesAsync();

        var body = new ClassResponse(cls.Id.ToString(), cls.Name, cls.TotalSeats, 0, 0, cls.TotalSeats);
        return Created($"/classes/{cls.Id}", body);
    }
}
