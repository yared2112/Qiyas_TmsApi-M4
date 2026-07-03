using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection; // 🚨 Crucial: Enables .CreateScope() extension method
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TmsApi;

public class EnrollmentWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory; // 🛡️ The remedy to prevent captive dependencies
    private readonly ILogger<EnrollmentWorker> _logger;

    // 🚨 CRITICAL: The constructor must accept IServiceScopeFactory, NOT IEnrollmentService!
    public EnrollmentWorker(IServiceScopeFactory scopeFactory, ILogger<EnrollmentWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("EnrollmentWorker is running scholarship recalculation...");

            // ⚡ Manually generate an isolated, short-lived processing boundary loop on this background thread
            using (var scope = _scopeFactory.CreateScope())
            {
                // Safely resolve the scoped service within the boundaries of this explicit using block
                var enrollmentService = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

                var enrollments = await enrollmentService.GetAllAsync();
                _logger.LogInformation("Currently tracking {Count} active enrollments in background.", enrollments.Count);
            }

            // Wake up every 10 seconds to execute processing sweeps
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
