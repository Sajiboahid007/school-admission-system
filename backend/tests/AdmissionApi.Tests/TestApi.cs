using System.Net.Http.Json;

namespace AdmissionApi.Tests;

public record ClassDto(string Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats);
public record HoldDto(string HoldId, string ClassId, string Status, DateTime ExpiresAt, string? StudentId = null, string? ClassName = null);
public record ErrorDto(string Error, string Message);

public static class TestApi
{
    // Every test uses new random ids and names, so tests don't affect each other.
    public static string NewStudentId() =>
        Random.Shared.NextInt64(10_000_000_000_000_000, 99_999_999_999_999_999).ToString();

    public static async Task<string> CreateClassAsync(HttpClient client, int seats)
    {
        var response = await client.PostAsJsonAsync("/classes",
            new { name = $"Test {Guid.NewGuid():N}", totalSeats = seats });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ClassDto>();
        return body!.Id;
    }

    public static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string url, string? studentId)
    {
        var request = new HttpRequestMessage(method, url);
        if (studentId is not null)
            request.Headers.TryAddWithoutValidation("X-Student-Id", studentId);
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> PostHoldAsync(HttpClient client, string classId, string studentId) =>
        SendAsync(client, HttpMethod.Post, $"/classes/{classId}/holds", studentId);

    public static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, string holdId, string studentId) =>
        SendAsync(client, HttpMethod.Post, $"/holds/{holdId}/confirm", studentId);

    public static async Task<ClassDto> GetClassAsync(HttpClient client, string classId) =>
        (await client.GetFromJsonAsync<ClassDto>($"/classes/{classId}"))!;
}
