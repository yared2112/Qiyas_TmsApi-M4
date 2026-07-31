using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi
{
    // Custom exception wrapper for simulating database infrastructure failures
    public class TmsDatabaseException(string message) : Exception(message);

    // 1. Structural Business Contract
    public interface IEnrollmentService
    {
        Task<Enrollment> EnrollAsync(int studentId, int courseId);
        Task<Enrollment?> GetByIdAsync(int id);
        Task<IReadOnlyList<Enrollment>> GetAllAsync();
        Task<bool> DeleteAsync(int id);
    }

    // 2. EF Core Implementation
    public class EnrollmentService : IEnrollmentService
    {
        private readonly TmsDbContext _context;
        private readonly ILogger<EnrollmentService> _logger;

        public EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Enrollment> EnrollAsync(int studentId, int courseId)
        {
            // Check for duplicate enrollment
            var existing = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);

            if (existing is not null)
            {
                _logger.LogWarning(
                    "Duplicate enrollment attempt Student {StudentId} already in Course {CourseId} (Enrollment {EnrollmentId})",
                    studentId, courseId, existing.Id);

                return existing;
            }

            var enrollment = new Enrollment
            {
                StudentId = studentId,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Enrolled Student {StudentId} in Course {CourseId} Enrollment {EnrollmentId}",
                studentId, courseId, enrollment.Id);

            return enrollment;
        }

        public async Task<Enrollment?> GetByIdAsync(int id)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment is null)
            {
                _logger.LogWarning("Enrollment {EnrollmentId} not found", id);
            }

            return enrollment;
        }

        public async Task<IReadOnlyList<Enrollment>> GetAllAsync()
        {
            return await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);

            if (enrollment is null)
            {
                _logger.LogWarning("Delete failed enrollment {EnrollmentId} not found", id);
                return false;
            }

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted enrollment {EnrollmentId}", id);
            return true;
        }
    }
}
