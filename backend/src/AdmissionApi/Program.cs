using AdmissionApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The connection string is read when the DbContext is created (not at startup),
// so tests can override it with UseSetting(). Env var: ConnectionStrings__Default.
builder.Services.AddDbContext<AdmissionDbContext>((sp, options) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    options.UseNpgsql(config.GetConnectionString("Default"))
           .UseSnakeCaseNamingConvention();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

await DbInitializer.MigrateAsync(app.Services, app.Logger);

app.Run();

// Needed so the test project can use WebApplicationFactory<Program>
public partial class Program { }
