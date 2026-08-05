using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;

namespace TmsApi.Services
{
    public class CourseService : ICourseService
    {
        private readonly TmsDbContext _context;

        public CourseService(TmsDbContext context)
        {
            _context = context;
        }

        public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
            _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CourseResponseDto(
                    c.Id,
                    c.Code,
                    c.Title,
                    c.MaxCapacity,
                    c.Enrollments.Count // ✅ critical for capacity checks
                ))
                .FirstOrDefaultAsync(ct);

        public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
        {
            var course = new Entities.Course
            {
                Code = request.Code,
                Title = request.Title,
                MaxCapacity = request.MaxCapacity
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync(ct);

            return (await GetByIdAsync(course.Id, ct))!;
        }

        public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
            _context.Courses.AnyAsync(c => c.Code == code, ct);

        public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
        {
            IQueryable<Entities.Course> query = _context.Courses.AsNoTracking();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(c =>
                    EF.Functions.ILike(c.Title, $"%{request.Search}%") ||
                    EF.Functions.ILike(c.Code, $"%{request.Search}%"));
            }

            // Apply sorting or count before paging
            var totalCount = await query.CountAsync(ct);

            // orderBy whitelist with Title as default
            query = request.OrderBy switch
            {
                "Code" => request.Descending
                ? query.OrderByDescending(c => c.Code)
                : query.OrderBy(c => c.Code),

                "MaxCapacity" => request.Descending
                ? query.OrderByDescending(c => c.MaxCapacity)
                : query.OrderBy(c => c.MaxCapacity),

                _ => request.Descending
                ? query.OrderByDescending(c => c.Title)
                : query.OrderBy(c => c.Title)
            };

            // Apply paging and projection to DTO
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new CourseResponseDto(
                    c.Id,
                    c.Code,
                    c.Title,
                    c.MaxCapacity,
                    c.Enrollments.Count))
                .ToListAsync(ct);

            return new PagedResponse<CourseResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

    }
}
