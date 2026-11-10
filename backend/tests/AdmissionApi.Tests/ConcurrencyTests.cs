using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AdmissionApi.Tests;

public class ConcurrencyTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ConcurrencyTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Many_parallel_holds_never_exceed_total_seats()
    {
        const int seats = 10;
        const int requests = 60;
        var client = _factory.CreateClient();
        var classId = await TestApi.CreateClassAsync(client, seats);

        // Fire all requests at the same time, each with a different student.
        var tasks = Enumerable.Range(0, requests)
            .Select(_ => TestApi.PostHoldAsync(client, classId, TestApi.NewStudentId()));
        var responses = await Task.WhenAll(tasks);

        Assert.Equal(seats, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(requests - seats, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);

        var cls = await TestApi.GetClassAsync(client, classId);
        Assert.Equal(seats, cls.HeldSeats + cls.ConfirmedSeats);
        Assert.Equal(0, cls.AvailableSeats);
    }

    [Fact]
    public async Task Same_student_cannot_hold_twice_in_same_class()
    {
        var client = _factory.CreateClient();
        var classId = await TestApi.CreateClassAsync(client, 5);
        var student = TestApi.NewStudentId();

        var results = await Task.WhenAll(
            TestApi.PostHoldAsync(client, classId, student),
            TestApi.PostHoldAsync(client, classId, student));

        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Confirm_twice_increases_confirmed_seats_once()
    {
        var client = _factory.CreateClient();
        var classId = await TestApi.CreateClassAsync(client, 5);
        var student = TestApi.NewStudentId();

        var created = await (await TestApi.PostHoldAsync(client, classId, student))
            .Content.ReadFromJsonAsync<HoldDto>();

        var results = await Task.WhenAll(
            TestApi.ConfirmAsync(client, created!.HoldId, student),
            TestApi.ConfirmAsync(client, created.HoldId, student),
            TestApi.ConfirmAsync(client, created.HoldId, student));

        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        var cls = await TestApi.GetClassAsync(client, classId);
        Assert.Equal(1, cls.ConfirmedSeats);
        Assert.Equal(0, cls.HeldSeats);
    }

    [Fact]
    public async Task Other_student_cannot_confirm()
    {
        var client = _factory.CreateClient();
        var classId = await TestApi.CreateClassAsync(client, 5);
        var studentA = TestApi.NewStudentId();
        var studentB = TestApi.NewStudentId();

        var created = await (await TestApi.PostHoldAsync(client, classId, studentA))
            .Content.ReadFromJsonAsync<HoldDto>();

        var response = await TestApi.ConfirmAsync(client, created!.HoldId, studentB);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var cls = await TestApi.GetClassAsync(client, classId);
        Assert.Equal(1, cls.HeldSeats);
        Assert.Equal(0, cls.ConfirmedSeats);
    }

    [Fact]
    public async Task Two_app_instances_on_same_db_never_oversell()
    {
        await using var appA = new ApiFactory();
        await using var appB = new ApiFactory();
        var clientA = appA.CreateClient();
        var clientB = appB.CreateClient();

        var classId = await TestApi.CreateClassAsync(clientA, 8);

        var tasks = Enumerable.Range(0, 40).Select(i =>
            TestApi.PostHoldAsync(i % 2 == 0 ? clientA : clientB, classId, TestApi.NewStudentId()));
        var responses = await Task.WhenAll(tasks);

        Assert.Equal(8, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
    }
}
