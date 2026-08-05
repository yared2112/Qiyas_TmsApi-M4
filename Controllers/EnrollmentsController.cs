using Microsoft.AspNetCore.Mvc;
using TmsApi.Dtos;
using TmsApi.Services;

namespace TmsApi.Controllers
{
    [ApiController]
    [Route("api/courses/{courseId:int}/enrollments")]
    public class EnrollmentsController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentsController(ICourseService courseService, IEnrollmentService enrollmentService)
        {
            _courseService = courseService;
            _enrollmentService = enrollmentService;
        }

        // 🔹 Single enrollment by ID
        [HttpGet("{id:int}", Name = nameof(GetEnrollment))]
        public async Task<IActionResult> GetEnrollment(int courseId, int id, CancellationToken ct)
        {
            var enrollment = await _enrollmentService.GetByIdAsync(id, ct);
            return enrollment is not null ? Ok(enrollment) : NotFound();
        }

        // 🔹 List all enrollments for a course
        [HttpGet(Name = "ListCourseEnrollments")]
        public async Task<IActionResult> GetEnrollments(int courseId, CancellationToken ct)
        {
            var course = await _courseService.GetByIdAsync(courseId, ct);
            if (course is null) return NotFound();

            var enrollments = await _enrollmentService.GetByCourseAsync(courseId, ct);
            return Ok(enrollments);
        }

        // 🔹 Enroll a student
        [HttpPost]
        public async Task<IActionResult> EnrollStudent(int courseId, EnrollStudentRequest request, CancellationToken ct)
        {
            // Confirm parent course exists
            var course = await _courseService.GetByIdAsync(courseId, ct);
            if (course is null) return NotFound();

            // Capacity check
            if (course.EnrollmentCount >= course.MaxCapacity)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Course is full",
                    Detail = $"Course '{course.Title}' has reached its maximum capacity of {course.MaxCapacity}.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            // Ensure request has the correct courseId
            request = request with { CourseId = courseId };

            // Proceed to enroll
            var enrollment = await _enrollmentService.CreateAsync(request, ct);

            return CreatedAtAction(
                nameof(GetEnrollment),
                new { courseId, id = enrollment.Id },
                enrollment
            );
        }
    }
}
