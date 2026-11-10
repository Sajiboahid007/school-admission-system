using System.ComponentModel.DataAnnotations;

namespace AdmissionApi.Dtos;

public class CreateClassRequest
{
    [Required(ErrorMessage = "Class name is required.")]
    [StringLength(100, ErrorMessage = "Class name must be at most 100 characters.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Total seats is required.")]
    [Range(1, 10000, ErrorMessage = "Total seats must be between 1 and 10000.")]
    public int? TotalSeats { get; set; }
}

public record ClassResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);
