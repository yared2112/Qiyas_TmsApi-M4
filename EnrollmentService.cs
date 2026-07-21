using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TmsApi;

// Standard custom exception wrapper used for simulating database infrastructure failures
public class TmsDatabaseException(string message) : Exception(message);

// 1. Structural Business Contract
public interface IEnrollmentService
{
    Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode);
    Task<EnrollmentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}

// 2. State-holding In-Memory Processing Implementation
public class EnrollmentService : IEnrollmentService
{
    private readonly Dictionary<string, EnrollmentRecord> _store = new();
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(ILogger<EnrollmentService> logger)
    {
        _logger = logger;
    }

    public Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode)
    {
        // 1. Audit Check: Intercept duplicate enrollment attempts
        var existing = _store.Values
            .FirstOrDefault(e => e.StudentId == studentId && e.CourseCode == courseCode);

        if (existing is not null)
        {
            //  LOG LEVEL: Warning - Unexpected but recoverable business workflow condition
            _logger.LogWarning(
                "Duplicate enrollment attempt {StudentId} already in {CourseCode} (record {EnrollmentId})",
                studentId, courseCode, existing.Id);

            return Task.FromResult(existing);
        }

        var id = Guid.NewGuid().ToString("N")[..8];
        var record = new EnrollmentRecord(id, studentId, courseCode, DateTime.UtcNow);
        _store[id] = record;

        //  LOG LEVEL: Information - A successful system transaction occurred
        _logger.LogInformation("Enrolled {StudentId} in {CourseCode} record {EnrollmentId}",
            studentId, courseCode, id);

        return Task.FromResult(record);
    }

    public Task<EnrollmentRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var record);

        if (record is null)
        {
            //  LOG LEVEL: Warning - Target lookups that return null states should be flagged for auditing
            _logger.LogWarning("Enrollment {EnrollmentId} not found", id);
        }

        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync()
    {
        IReadOnlyList<EnrollmentRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);

        if (removed)
        {
            //  LOG LEVEL: Information - State destruction completed successfully
            _logger.LogInformation("Deleted enrollment {EnrollmentId}", id);
        }
        else
        {
            //  LOG LEVEL: Warning - Attempted deletion on an unresolvable domain entity
            _logger.LogWarning("Delete failed enrollment {EnrollmentId} not found", id);
        }

        return Task.FromResult(removed);
    }
}

// 3. Immutable Data Manifest DTO Shape
public record EnrollmentRecord(
    string Id,
    string StudentId,
    string CourseCode,
    DateTime EnrolledAt);
