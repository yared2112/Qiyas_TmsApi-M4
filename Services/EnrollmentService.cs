using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly TmsDbContext _context;
        private readonly ILogger<EnrollmentService> _logger;

        public EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task<EnrollmentResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
            _context.Enrollments
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new EnrollmentResponseDto(
                    e.Id,
                    e.CourseId,
                    e.StudentId,
                    e.EnrolledAt
                ))
                .FirstOrDefaultAsync(ct);

        public async Task<EnrollmentResponseDto> CreateAsync(EnrollStudentRequest request, CancellationToken ct)
        {
            var enrollment = new Enrollment
            {
                CourseId = request.CourseId,
                StudentId = request.StudentId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Student {StudentId} enrolled in course {CourseId}", request.StudentId, request.CourseId);

            return (await GetByIdAsync(enrollment.Id, ct))!;
        }

        public async Task<List<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct)
        {
            return await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseId == courseId)
                .Select(e => new EnrollmentResponseDto(
                    e.Id,
                    e.CourseId,
                    e.StudentId,
                    e.EnrolledAt))
                .ToListAsync(ct);
        }
    }
}
