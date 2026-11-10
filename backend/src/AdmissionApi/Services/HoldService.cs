using AdmissionApi.Common;
using AdmissionApi.Data;
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
}
