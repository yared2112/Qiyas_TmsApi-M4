using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TmsApi;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Generate an isolated, compact, unique tracking footprint
        string correlationId = Guid.NewGuid().ToString("N")[..8].ToUpper();

        // 2. Inject the tracker stamp into the outbound HTTP response header table
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // 3. Initiate the processing precision diagnostic timer
        var sw = Stopwatch.StartNew();

        // 4. Log Entry Telemetry Trace
        _logger.LogInformation("--> [HTTP START] Method: {Method} | Path: {Path} | CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        try
        {
            // Pass the transaction context further down the middleware pipeline
            await _next(context);
        }
        finally
        {
            // 5. Terminate timer and capture Log Exit Telemetry Trace under identical ID
            sw.Stop();
            _logger.LogInformation("<-- [HTTP END] Status: {StatusCode} | Duration: {Elapsed}ms | CorrelationId: {CorrelationId}",
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                correlationId);
        }
    }
}
