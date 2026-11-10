using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AdmissionApi.Tests;

public class ExpiryTests : IClassFixture<ShortHoldApiFactory>
{
    private readonly HttpClient _client;
    public ExpiryTests(ShortHoldApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Expired_hold_is_reported_and_seat_comes_back()
    {
        var classId = await TestApi.CreateClassAsync(_client, 1);
        var student = TestApi.NewStudentId();
        var hold = await (await TestApi.PostHoldAsync(_client, classId, student))
            .Content.ReadFromJsonAsync<HoldDto>();

        // class is full now
        var other = await TestApi.PostHoldAsync(_client, classId, TestApi.NewStudentId());
        Assert.Equal(HttpStatusCode.Conflict, other.StatusCode);

        await Task.Delay(TimeSpan.FromSeconds(3)); // hold duration is 2 s

        var status = await _client.GetFromJsonAsync<HoldDto>($"/holds/{hold!.HoldId}");
        Assert.Equal("EXPIRED", status!.Status);

        var confirm = await TestApi.ConfirmAsync(_client, hold.HoldId, student);
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);

        // nobody called any API to release the seat, but it is free again
        var cls = await TestApi.GetClassAsync(_client, classId);
        Assert.Equal(1, cls.AvailableSeats);

        var again = await TestApi.PostHoldAsync(_client, classId, TestApi.NewStudentId());
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task Cancel_returns_204_and_frees_the_seat()
    {
        var classId = await TestApi.CreateClassAsync(_client, 1);
        var student = TestApi.NewStudentId();
        var hold = await (await TestApi.PostHoldAsync(_client, classId, student))
            .Content.ReadFromJsonAsync<HoldDto>();

        var cancel = await TestApi.SendAsync(_client, HttpMethod.Delete, $"/holds/{hold!.HoldId}", student);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var cls = await TestApi.GetClassAsync(_client, classId);
        Assert.Equal(1, cls.AvailableSeats);

        // Delete again should still be 204
        var cancelAgain = await TestApi.SendAsync(_client, HttpMethod.Delete, $"/holds/{hold.HoldId}", student);
        Assert.Equal(HttpStatusCode.NoContent, cancelAgain.StatusCode);

        // Confirm after cancel should be 409
        var confirm = await TestApi.ConfirmAsync(_client, hold.HoldId, student);
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
    }
}
