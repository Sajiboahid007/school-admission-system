namespace AdmissionApi.Models;

// "Class" is a C# keyword, so the entity is called SchoolClass.
// One row = one class + shift, e.g. "Class 6 - Morning Shift".
public class SchoolClass
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int TotalSeats { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<Hold> Holds { get; set; } = new();
}
