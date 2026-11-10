using AdmissionApi.Common;
using AdmissionApi.Data;
using AdmissionApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var message = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? "The request is not valid.";

            return new BadRequestObjectResult(new ErrorResponse("VALIDATION_ERROR", message));
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// The connection string is read when the DbContext is created (not at startup),
// so tests can override it with UseSetting(). Env var: ConnectionStrings__Default.
builder.Services.AddDbContext<AdmissionDbContext>((sp, options) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    options.UseNpgsql(config.GetConnectionString("Default"))
           .UseSnakeCaseNamingConvention();
});

builder.Services.AddOptions<HoldOptions>()
    .Configure<IConfiguration>((options, config) =>
    {
        var raw = config["HOLD_DURATION_SECONDS"];
        options.DurationSeconds = int.TryParse(raw, out var seconds) && seconds > 0 ? seconds : 600;
    });

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<HoldService>();
builder.Services.AddHostedService<HoldExpiryWorker>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

await DbInitializer.MigrateAsync(app.Services, app.Logger);

app.Run();

// Needed so the test project can use WebApplicationFactory<Program>
public partial class Program { }
