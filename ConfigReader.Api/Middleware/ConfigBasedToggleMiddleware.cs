using ConfigReader.Api.Models;
using Microsoft.Extensions.Options;
using System.Net;

namespace ConfigReader.Api.Middleware;

/// <summary>
/// Config-based toggle middleware - API'yi enable/disable eder
/// </summary>
public sealed class ConfigBasedToggleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ConfigBasedToggleMiddleware> _logger;
    private readonly ConfigReaderApiOptions _options;

    /// <summary>
    /// Toggle kontrolü yapılacak endpoint'ler
    /// </summary>
    private static readonly string[] ToggleEndpoints = 
    {
        "/api/configuration",
        "/api/configuration/environment",
        "/api/configuration/appsettings"
    };

    /// <summary>
    /// Toggle kontrolü yapılmayacak endpoint'ler (token endpoint'leri)
    /// </summary>
    private static readonly string[] ExcludedEndpoints = 
    {
        "/api/token"
    };

    public ConfigBasedToggleMiddleware(
        RequestDelegate next,
        ILogger<ConfigBasedToggleMiddleware> logger,
        IOptions<ConfigReaderApiOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Middleware invoke metodu
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Toggle kontrolü yapılacak endpoint mi?
        if (!ShouldCheckToggle(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // API devre dışı mı?
        if (!_options.IsEnabled)
        {
            await HandleDisabledApiAsync(context);
            return;
        }

        // API aktif, devam et
        _logger.LogDebug("API is enabled, proceeding with request to {Path}", context.Request.Path);
        await _next(context);
    }

    /// <summary>
    /// Toggle kontrolü yapılıp yapılmayacağını belirler
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>Toggle kontrolü yapılacak ise true</returns>
    private static bool ShouldCheckToggle(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Excluded endpoint'leri kontrol et
        if (ExcludedEndpoints.Any(endpoint => 
            path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return ToggleEndpoints.Any(endpoint => 
            path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// API devre dışı olduğunda response döner
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    private async Task HandleDisabledApiAsync(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);
        
        _logger.LogWarning("API is disabled, blocking request from {ClientIp} to {Path}", 
            clientIp, context.Request.Path);

        context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "Service Unavailable",
            message = "ConfigReader API is currently disabled",
            statusCode = 503,
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
    }

    /// <summary>
    /// Client IP adresini alır
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Client IP adresi</returns>
    private static string GetClientIpAddress(HttpContext context)
    {
        // X-Forwarded-For header'ından IP'yi al
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        // X-Real-IP header'ından IP'yi al
        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        // RemoteIpAddress'i kullan
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
} 