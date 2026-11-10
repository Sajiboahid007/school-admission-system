using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AdmissionApi.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    // Separate database so tests never touch your dev data.
    // In Docker the test service sets TEST_DB_CONNECTION (host = "db").
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Host=localhost;Port=5433;Database=admission_test;Username=admission;Password=admission";

    protected virtual int HoldSeconds => 600;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("HOLD_DURATION_SECONDS", HoldSeconds.ToString());
    }
}

// Same app, but holds expire after 2 seconds.
public class ShortHoldApiFactory : ApiFactory
{
    protected override int HoldSeconds => 2;
}
