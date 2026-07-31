using Microsoft.AspNetCore.Mvc;
using TmsApi.Dtos;
using TmsApi.Services;

namespace TmsApi.Controllers
{
    [ApiController]
    [Route("api/courses")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CoursesController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        [HttpGet("{id:int}", Name = nameof(GetCourseById))]
        public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
        {
            var course = await _courseService.GetByIdAsync(id, ct);
            return course is not null ? Ok(course) : NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse(CreateCourseRequest request, CancellationToken ct)
        {
            // Step 1: Check if the code already exists
            if (await _courseService.CodeExistsAsync(request.Code, ct))
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Course code already exists",
                    Detail = $"A course with code '{request.Code}' is already registered.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            // Step 2: If not duplicate, create the course
            var result = await _courseService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
        }
    }
}
