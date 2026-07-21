using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly TmsDbContext _context;
    public TestController(TmsDbContext context) => _context = context;

    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>>> STEP 1: Building query object...");
        var query = _context.Students.Where(s => s.GPA >= 3.0m);

        Console.WriteLine(">>> STEP 2: Adding sorting...");
        var orderedQuery = query.OrderBy(s => s.Name);

        Console.WriteLine(">>> STEP 3: Materializing query...");
        var results = orderedQuery.ToList(); // SQL runs here

        Console.WriteLine(">>> STEP 4: Done.\n");
        return Ok(results);
    }
    private static bool IsHonorRoll(decimal gpa) => gpa >= 3.5m;

    // -------------------------------
    // STEP 4: Translation Fail Demo
    // -------------------------------

    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");
        try
        {
            var students = _context.Students
                .Where(s => IsHonorRoll(s.GPA)) // EF Core cannot translate this
                .ToList();

            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION: {ex.Message}\n");
            return BadRequest(new { Message = ex.Message });
        }
    }

    // Server-side fix (preferred)
    [HttpGet("translation-fail-fixed-server")]
    public IActionResult TranslationFailFixedServer()
    {
        Console.WriteLine("\n>>> Running server-side evaluation...");
        var students = _context.Students
            .Where(s => s.GPA >= 3.5m) // inline logic, EF Core can translate
            .ToList();

        return Ok(students);
    }

    // Client-side fix (fallback)
    [HttpGet("translation-fail-fixed-client")]
    public IActionResult TranslationFailFixedClient()
    {
        Console.WriteLine("\n>>> Running client-side evaluation...");
        var students = _context.Students
            .AsEnumerable() // pull all rows into memory
            .Where(s => IsHonorRoll(s.GPA)) // apply C# logic locally
            .ToList();

        return Ok(students);
    }

    // -------------------------------
    // STEP 5: Registrar’s Business Queries
    // -------------------------------

    // 1. Active Students with GPA ≥ 3.0
    [HttpGet("active-count")]
    public async Task<IActionResult> ActiveStudentCount()
    {
        var count = await _context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();

        return Ok(count);
    }

    // 2. Courses with Most Enrollments
    [HttpGet("top-courses")]
    public async Task<IActionResult> TopCourses()
    {
        var list = await _context.Courses
            .Select(c => new { c.Title, EnrollmentCount = c.Enrollments.Count })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();

        return Ok(list);
    }

    // 3. Average GPA per Course
    [HttpGet("avg-gpa")]
    public async Task<IActionResult> AverageGpaPerCourse()
    {
        var list = await _context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new { Course = g.Key, AverageGPA = g.Average(e => e.Student.GPA) })
            .ToListAsync();

        return Ok(list);
    }

    // 4. Students with Zero Enrollments
    [HttpGet("no-enrollments")]
    public async Task<IActionResult> StudentsWithNoEnrollments()
    {
        var list = await _context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();

        return Ok(list);
    }
}
