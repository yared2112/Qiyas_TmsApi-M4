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
using Microsoft.EntityFrameworkCore; // Required for DbContextOptions
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi;

var builder = WebApplication.CreateBuilder(args);

// Register TmsDbContext scoped for incoming HTTP requests
builder.Services.AddDbContext<TmsDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
       .LogTo(Console.WriteLine, LogLevel.Information) // Log SQL to output window
       .EnableSensitiveDataLogging());                 // Show parameters in query logs (dev only)

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

//  TODO 1, 2 & 3: Environment Toggles and Pipeline Splitting 
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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    context.Database.Migrate();

    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
        };
        context.Students.AddRange(students);

        var courses = new List<Course>
        {
            new() { Code = "CS-101", Title = "Intro to Computer Science", Capacity = 30 },
            new() { Code = "CS-201", Title = "Data Structures", Capacity = 25 },
            new() { Code = "MAT-101", Title = "Calculus I", Capacity = 40 }
        };
        context.Courses.AddRange(courses);
        context.SaveChanges();

        var enrollments = new List<Enrollment>
        {
            new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
            new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
            new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
            new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
        };
        context.Enrollments.AddRange(enrollments);
        context.SaveChanges();
    }
}

app.Run();


// === MOCK AUTHENTICATION STUB COMPONENT ===
public class MockAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public MockAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder) : base(options, logger, encoder) { }
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
}
