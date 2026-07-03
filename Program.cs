using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TmsApi; // RequestLoggingMiddleware ን ለማግኘት

var builder = WebApplication.CreateBuilder(args);

// === 1. SERVICES CONFIGURATION (Dependency Injection) ===

// 🛡️ የቅንብሮች አረጋጋጭ (Options Validation on Startup)
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 🛡️ የተስተካከለ የአገልግሎቶች ምዝገባ (Fixed Lifetimes) - የኮንቴይነሩ ስህተት ተፈውሷል
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EnrollmentWorker>());

// 🛡️ የኮንቴይነር መከላከያ ኦዲት ህግ (ሁልጊዜ ክፍት ሆኖ ይቀጥላል)
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// 🛡️ የደህንነት (Authentication/Authorization) ምዝገባ
builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, MockAuthHandler>("Bearer", null);
builder.Services.AddAuthorization();

var app = builder.Build();


// === 2. HTTP PIPELINE MIDDLEWARE SEQUENCING ===

// ህግ 1፦ የጥያቄዎች ሎጊንግ (ከሁሉም ውጪ ይሆንና ሙሉ ጊዜውን ይለካል)
app.UseMiddleware<RequestLoggingMiddleware>();

// ህግ 2፦ ያልተያዙ ስህተቶች መከላከያ 
app.UseExceptionHandler("/error");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


// === 3. ENDPOINT MAPPINGS (የኤፒአይ መድረሻዎች) ===

// ሀ. የተጠበቀው የውጤት ኤንድፖይንት (የቀድሞው ህግ - 401 ይጥላል)
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

// ለ. POST: አዲስ ተማሪ መመዝገብ (Structured Logging ለመፈተሽ)
app.MapPost("/api/enrollments", async (string studentId, string courseCode, IEnrollmentService service) =>
{
    var record = await service.EnrollAsync(studentId, courseCode);
    return Results.Ok(record);
}).AllowAnonymous();

// ሐ. GET: የተመዘገበ ተማሪን በመታወቂያ መፈለግ (Warning ለመፈተሽ)
app.MapGet("/api/enrollments/{id}", async (string id, IEnrollmentService service) =>
{
    var record = await service.GetByIdAsync(id);
    return record is not null ? Results.Ok(record) : Results.NotFound();
}).AllowAnonymous();

// መ. DELETE: የተመዘገበ ተማሪን መዝገብ መሰረዝ (Information/Warning ለመፈተሽ)
app.MapDelete("/api/enrollments/{id}", async (string id, IEnrollmentService service) =>
{
    var success = await service.DeleteAsync(id);
    return success ? Results.NoContent() : Results.NotFound();
}).AllowAnonymous();


app.Run();


// === MOCK AUTHENTICATION STUB COMPONENT ===
public class MockAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public MockAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
