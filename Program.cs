using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore; // Required for Scalar middleware
using TmsApi;

var builder = WebApplication.CreateBuilder(args);

// === 1. SERVICES INJECTION CONTAINER CONFIGURATION ===
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Required before calling MapOpenApi() later in the pipeline
builder.Services.AddOpenApi();

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EnrollmentWorker>());

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, MockAuthHandler>("Bearer", null);
builder.Services.AddAuthorization();

var app = builder.Build();

// === 2. HTTP PIPELINE MIDDLEWARE SEQUENCING ===

// Custom request logging executes at the boundary in all environment loops
app.UseMiddleware<RequestLoggingMiddleware>();

// 🚨 TODO 1, 2 & 3: Environment Toggles and Pipeline Splitting 🚨
if (app.Environment.IsDevelopment())
{
    // TODO 2: Expose OpenAPI document and Scalar UI in Development only
    app.MapOpenApi();              // JSON schema file endpoint -> /openapi/v1.json
    app.MapScalarApiReference();   // Interactive web explorer panel -> /scalar/v1

    // Rich in-app diagnostics for developer workflows
    app.UseDeveloperExceptionPage();
}
else
{
    // TODO 3: Mask stack traces securely using standard Exception Handler in Production
    app.UseExceptionHandler();
}

app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// === 3. CONTROLLER AND ENDPOINT ROUTING ALLOCATIONS ===
app.MapControllers();

app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

app.Run();


// === MOCK AUTHENTICATION STUB COMPONENT ===
public class MockAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public MockAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder) : base(options, logger, encoder) { }
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
}
