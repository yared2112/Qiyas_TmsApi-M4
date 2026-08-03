using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using TmsApi.Data;
using TmsApi.Services;
using TmsApi.Persistence;
using TmsApi;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 1. Add DbContext with PostgreSQL + EF Core SQL logging routed to Serilog
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDb"))
           .EnableSensitiveDataLogging()
           .LogTo(Log.Information, LogLevel.Information));

// 2. Register services
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICourseService, CourseService>();

// 3. Add controllers with global audit filter
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
});

// 4. Swagger/OpenAPI
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

// 🔹 Development-only middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TMS API v1");
        c.RoutePrefix = string.Empty;
    });
    app.UseDeveloperExceptionPage();

    // Run seeder
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    await DataSeeder.SeedAsync(context);
}

// Only enforce HTTPS redirection outside of local development environments
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();

// 5. Map endpoints
app.MapControllers();
app.MapGet("/", () => "TMS API is running");

app.Run();
