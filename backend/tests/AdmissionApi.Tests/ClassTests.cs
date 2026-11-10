using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace AdmissionApi.Tests;

public class ClassTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    public ClassTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Create_class_returns_201_with_contract_fields()
    {
        var name = $"Class 6 - Morning {Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/classes", new { name, totalSeats = 120 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ClassDto>();
        Assert.False(string.IsNullOrEmpty(body!.Id));
        Assert.Equal(name, body.Name);
        Assert.Equal(120, body.TotalSeats);
    }

    [Theory]
    [InlineData("{\"name\":\"\",\"totalSeats\":10}")]
    [InlineData("{\"name\":\"A\",\"totalSeats\":0}")]
    [InlineData("{\"name\":\"A\",\"totalSeats\":-5}")]
    [InlineData("{\"name\":\"A\",\"totalSeats\":\"abc\"}")]
    [InlineData("{\"name\":\"A\"}")]
    [InlineData("{broken json")]
    public async Task Invalid_class_input_returns_400(string json)
    {
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/classes", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("123")]
    [InlineData("1234567890123456a")]
    [InlineData("২০১৫২৬৯১২৩৪৫৬৭৮৯০")] // Bangla digits
    public async Task Bad_student_id_returns_400(string? studentId)
    {
        var classId = await TestApi.CreateClassAsync(_client, 5);
        var response = await TestApi.PostHoldAsync(_client, classId, studentId!);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
