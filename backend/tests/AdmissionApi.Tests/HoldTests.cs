using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AdmissionApi.Tests;

public class HoldTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    public HoldTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Me_holds_lists_only_this_students_holds()
    {
        var classId1 = await TestApi.CreateClassAsync(_client, 5);
        var classId2 = await TestApi.CreateClassAsync(_client, 5);

        var student1 = TestApi.NewStudentId();
        var student2 = TestApi.NewStudentId();

        await TestApi.PostHoldAsync(_client, classId1, student1);
        await TestApi.PostHoldAsync(_client, classId2, student1);
        await TestApi.PostHoldAsync(_client, classId1, student2);

        var response = await TestApi.SendAsync(_client, HttpMethod.Get, "/me/holds", student1);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var holds = await response.Content.ReadFromJsonAsync<List<HoldDto>>();
        Assert.NotNull(holds);
        Assert.Equal(2, holds.Count);
        Assert.All(holds, h => Assert.False(string.IsNullOrEmpty(h.HoldId)));
    }
}
