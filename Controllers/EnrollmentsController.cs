using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Entities;

namespace TmsApi.Controllers
{
    [ApiController]
    [Route("api/enrollments")]
    [AllowAnonymous]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentsController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

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
            var enrollments = await _enrollmentService.GetAllAsync();
            return Ok(enrollments);
        }

        // GET /api/enrollments/{id} -> Returns 200 OK or 404 Not Found
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var record = await _enrollmentService.GetByIdAsync(id);
            return record is not null ? Ok(record) : NotFound();
        }

        // POST /api/enrollments -> Creates entity and yields 201 Created with Location header
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest request)
        {
            var record = await _enrollmentService.EnrollAsync(request.StudentId, request.CourseId);

            // Generates an HTTP response with a 201 status and auto-computes the outbound URI location
            return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
        }

        // DELETE /api/enrollments/{id} -> Returns 204 No Content or 404 Not Found
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _enrollmentService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }

    // Data Transfer Object (DTO) for handling client creation body data payloads
    public record CreateEnrollmentRequest(int StudentId, int CourseId);
}
