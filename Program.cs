using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using TmsApi.Data;
using TmsApi;


var builder = WebApplication.CreateBuilder(args);

// 1. Add DbContext with PostgreSQL
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDb")));

// 2. Register EnrollmentService
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// 3. Add controllers if you’re using MVC
builder.Services.AddControllers();

// 3. Add Minimal API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TMS API",
        Version = "v1",
        Description = "Training Management System API"
    });
});

var app = builder.Build();

// 4. Configure middleware (optional extras)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TMS API v1");
        c.RoutePrefix = string.Empty; // serve Swagger UI at root (https://localhost:5247/)
    });
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// 5. Map endpoints
app.MapControllers(); // If using MVC controllers
// Minimal API example
app.MapGet("/", () => "TMS API is running");

// Get all enrollments
app.MapGet("/enrollments", async (IEnrollmentService service) =>
{
    return await service.GetAllAsync();
});

// Get enrollment by Id
app.MapGet("/enrollments/{id:int}", async (int id, IEnrollmentService service) =>
{
    var record = await service.GetByIdAsync(id);
    return record is not null ? Results.Ok(record) : Results.NotFound();
});

// Enroll a student in a course
app.MapPost("/enrollments", async (int studentId, int courseCode, IEnrollmentService service) =>
{
    var record = await service.EnrollAsync(studentId, courseCode); // Parse studentId, courseCode);
    return Results.Created($"/enrollments/{record.Id}", record);
});

// Delete enrollment
app.MapDelete("/enrollments/{id:int}", async (int id, IEnrollmentService service) =>
{
    var success = await service.DeleteAsync(id);
    return success ? Results.Ok() : Results.NotFound();
});

app.Run();
