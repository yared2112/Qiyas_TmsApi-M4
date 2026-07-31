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


        [HttpGet("{id:int}", Name = nameof(GetEnrollment))]
        public async Task<IActionResult> GetEnrollment(int courseId, int id, CancellationToken ct)
        {
            var enrollment = await _enrollmentService.GetByIdAsync(courseId, id, ct);
            return enrollment is not null ? Ok(enrollment) : NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> EnrollStudent(int courseId, EnrollStudentRequest request, CancellationToken ct)
        {
            // Check if the course exists in the parent table
            var course = await _courseService.GetByIdAsync(courseId, ct);
            if (course is null) return NotFound();

            // Check if the course is full capacity before enrolling the student    
            if (course.EnrollmentCount >= course.MaxCapacity)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Course is full",
                    Detail = $"Course '{course.Title}' has reached its maximum capacity of {course.MaxCapacity}.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            // Proceed to enroll the student
            var enrollment = await _enrollmentService.CreateAsync(courseId, request, ct);
            return CreatedAtAction(nameof(GetEnrollment), new { courseId, id = enrollment.Id }, enrollment);
        }

    }
}
