namespace AdmissionApi.Models;

public class Hold
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public SchoolClass? Class { get; set; }

    // 17-digit birth registration number
    public string StudentId { get; set; } = "";
    public HoldStatus Status { get; set; }

    // All times are UTC (Npgsql requires DateTimeKind.Utc for timestamptz).
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
