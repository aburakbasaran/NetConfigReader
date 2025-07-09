using System.Text;

namespace ConfigReader.Api.Middleware;

/// <summary>
/// Sensitive endpoint'lerin response'larını log'lamayı engelleyen middleware
/// </summary>
public sealed class SensitiveEndpointLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SensitiveEndpointLoggingMiddleware> _logger;

    /// <summary>
    /// Sensitive endpoint'ler - response'ları log'lanmaması gereken
    /// </summary>
    private static readonly string[] SensitiveEndpoints = 
    {
        "/api/configuration",
        "/api/configuration/environment",
        "/api/configuration/appsettings"
    };

    public SensitiveEndpointLoggingMiddleware(
        RequestDelegate next,
        ILogger<SensitiveEndpointLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Middleware invoke metodu
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Sensitive endpoint mi kontrol et
        if (IsSensitiveEndpoint(context.Request.Path))
        {
            // Response logging'i disable et
            await InvokeWithoutLoggingAsync(context);
        }
        else
        {
            // Normal işleme devam et
            await _next(context);
        }
    }

    /// <summary>
    /// Sensitive endpoint olup olmadığını kontrol eder
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>Sensitive endpoint ise true</returns>
    private static bool IsSensitiveEndpoint(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return SensitiveEndpoints.Any(endpoint => 
            path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Response logging olmadan request'i işler
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    private async Task InvokeWithoutLoggingAsync(HttpContext context)
    {
        // Original response stream'i sakla
        var originalBodyStream = context.Response.Body;

        try
        {
            // Memory stream ile response'u yakala ama log'lama
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Request'i işle
            await _next(context);

            // Response'u original stream'e kopyala (log'lamadan)
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await context.Response.Body.CopyToAsync(originalBodyStream);

            // Sadece request bilgilerini log'la (response'u değil)
            LogSensitiveEndpointAccess(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sensitive endpoint {Path}", context.Request.Path);
            throw;
        }
        finally
        {
            // Original stream'i geri yükle
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// Sensitive endpoint erişimini log'lar (response içeriği olmadan)
    /// </summary>
    /// <param name="context">HTTP context</param>
    private void LogSensitiveEndpointAccess(HttpContext context)
    {
        var request = context.Request;
        var response = context.Response;

        // Sadece temel bilgileri log'la
        _logger.LogInformation(
            "Sensitive endpoint accessed: {Method} {Path} - Status: {StatusCode} - IP: {RemoteIpAddress} - UserAgent: {UserAgent}",
            request.Method,
            request.Path,
            response.StatusCode,
            GetClientIpAddress(context),
            request.Headers["User-Agent"].ToString()
        );

        // Response body'sini ASLA log'lama
        _logger.LogDebug("Response body logging disabled for sensitive endpoint {Path}", request.Path);
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