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
        private readonly LinkGenerator _linkGenerator;

        public CoursesController(ICourseService courseService, LinkGenerator linkGenerator)
        {
            _courseService = courseService;
            _linkGenerator = linkGenerator;
        }

        // [HttpGet("{id:int}", Name = nameof(GetCourseById))]
        // public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
        // {
        //     var course = await _courseService.GetByIdAsync(id, ct);
        //     return course is not null ? Ok(course) : NotFound();
        // }

        [HttpGet("{id:int}", Name = nameof(GetCourseById))]
        public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
        {
            var course = await _courseService.GetByIdAsync(id, ct);
            if (course is null) return NotFound();

            // Build links
            var links = new List<LinkDto>
    {
        new LinkDto(
            Href: _linkGenerator.GetPathByName(HttpContext, nameof(GetCourseById), new { id })!,
            Rel: "self",
            Method: "GET"),

        new LinkDto(
            Href: _linkGenerator.GetPathByName(HttpContext, nameof(GetCourseById), new { id })!,
            Rel: "update",
            Method: "PUT"),

        new LinkDto(
            Href: _linkGenerator.GetPathByName(HttpContext, nameof(GetCourseById), new { id })!,
            Rel: "delete",
            Method: "DELETE"),

        new LinkDto(
            Href: _linkGenerator.GetPathByAction(HttpContext,
                action: "GetEnrollments",
                controller: "Enrollments",
                values: new { courseId = id })!,
            Rel: "enrollments",
            Method: "GET")
    };

            // Conditional enroll link
            if (course.EnrollmentCount < course.MaxCapacity)
            {
                links.Add(new LinkDto(
                    Href: _linkGenerator.GetPathByAction(HttpContext,
                        action: "GetEnrollments",
                        controller: "Enrollments",
                        values: new { courseId = id })!,
                    Rel: "enroll",
                    Method: "POST"));
            }

            var detailDto = new CourseDetailDto
            {
                Id = course.Id,
                Code = course.Code,
                Title = course.Title,
                MaxCapacity = course.MaxCapacity,
                EnrollmentCount = course.EnrollmentCount,
                Links = links
            };

            return Ok(detailDto);
        }


        [HttpPost]
        public async Task<IActionResult> CreateCourse(CreateCourseRequest request, CancellationToken ct)
        {
            if (await _courseService.CodeExistsAsync(request.Code, ct))
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Course code already exists",
                    Detail = $"A course with code '{request.Code}' is already registered.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            var result = await _courseService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
        }

        //🔹 New Paginated GET
        [HttpGet]
        public async Task<IActionResult> GetCourses([FromQuery] PagedRequest request, CancellationToken ct)
        {
            var result = await _courseService.GetCoursesAsync(request, ct);
            return Ok(result);
        }
    }
}
