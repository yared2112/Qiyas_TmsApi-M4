using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/enrollments")]
[AllowAnonymous]
public class EnrollmentsController(IEnrollmentService enrollmentService) : ControllerBase
{
    // 🚨 FIXED: Placed safely inside the class block boundary 🚨
    // GET /api/enrollments/error -> Intentionally crashes to test 500 ProblemDetails
    [HttpGet("error")]
    public IActionResult TriggerError()
    {
        throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
    }

    // GET /api/enrollments -> Returns 200 OK with all records
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var enrollments = await enrollmentService.GetAllAsync();
        return Ok(enrollments);
    }

    // GET /api/enrollments/{id} -> Returns 200 OK or 404 Not Found
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var record = await enrollmentService.GetByIdAsync(id);
        return record is not null ? Ok(record) : NotFound();
    }

    // POST /api/enrollments -> Creates entity and yields 201 Created with Location header
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest request)
    {
        var record = await enrollmentService.EnrollAsync(request.StudentId, request.CourseCode);

        // Generates an HTTP response with a 201 status and auto-computes the outbound URI location
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    // DELETE /api/enrollments/{id} -> Returns 204 No Content or 404 Not Found
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await enrollmentService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}

// Data Transfer Object (DTO) for handling client creation body data payloads
public record CreateEnrollmentRequest(string StudentId, string CourseCode);
