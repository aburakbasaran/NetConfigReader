using ConfigReader.Api.Services;
using System.Net;

namespace ConfigReader.Api.Middleware;

/// <summary>
/// Rate limiting middleware
/// </summary>
public sealed class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<RateLimitMiddleware> _logger;

    /// <summary>
    /// Rate limiting için korumalı path'ler
    /// </summary>
    private static readonly string[] ProtectedPaths = 
    {
        "/api/configuration",
        "/api/configuration/environment",
        "/api/configuration/appsettings"
    };

    public RateLimitMiddleware(
        RequestDelegate next,
        IRateLimitService rateLimitService,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _rateLimitService = rateLimitService ?? throw new ArgumentNullException(nameof(rateLimitService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Middleware invoke metodu
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Health check ve diğer endpoint'leri atla
        if (!ShouldApplyRateLimit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            var clientId = GetClientId(context);
            var endpoint = GetEndpointName(context.Request.Path);

            // Rate limit kontrol et
            var isAllowed = await _rateLimitService.IsRequestAllowedAsync(clientId, endpoint);

            if (!isAllowed)
            {
                await HandleRateLimitExceededAsync(context, clientId, endpoint);
                return;
            }

            // Rate limit header'larını ekle
            await AddRateLimitHeadersAsync(context, clientId, endpoint);

            // İsteği devam ettir
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rate limit middleware error for path {Path}", context.Request.Path);
            // Hata durumunda isteği devam ettir
            await _next(context);
        }
    }

    /// <summary>
    /// Rate limit uygulanıp uygulanmayacağını kontrol eder
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>Rate limit uygulanacak ise true</returns>
    private static bool ShouldApplyRateLimit(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return ProtectedPaths.Any(protectedPath => 
            path.StartsWith(protectedPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Client ID'yi çıkarır (IP address kullanır)
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Client ID</returns>
    private static string GetClientId(HttpContext context)
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

    /// <summary>
    /// Endpoint adını çıkarır
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>Endpoint adı</returns>
    private static string GetEndpointName(string path)
    {
        // Path'i normalize et
        path = path.ToLowerInvariant();
        
        if (path.StartsWith("/api/configuration/environment"))
            return "environment";
        
        if (path.StartsWith("/api/configuration/appsettings"))
            return "appsettings";
        
        if (path.StartsWith("/api/configuration/"))
            return "configuration-specific";
        
        if (path.StartsWith("/api/configuration"))
            return "configuration-all";
        
        return "unknown";
    }

    /// <summary>
    /// Rate limit aşıldığında response döner
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="clientId">Client ID</param>
    /// <param name="endpoint">Endpoint</param>
    /// <returns>Task</returns>
    private async Task HandleRateLimitExceededAsync(HttpContext context, string clientId, string endpoint)
    {
        var resetTime = await _rateLimitService.GetResetTimeAsync(clientId, endpoint);
        var retryAfter = (int)(resetTime - DateTime.UtcNow).TotalSeconds;

        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.Headers["Retry-After"] = retryAfter.ToString();
        context.Response.Headers["X-RateLimit-Limit"] = "10";
        context.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.Response.Headers["X-RateLimit-Reset"] = resetTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var errorResponse = new
        {
            error = "Rate limit exceeded",
            message = "Daily request limit of 10 exceeded for this endpoint",
            retryAfter = retryAfter,
            resetTime = resetTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}", clientId, endpoint);

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
    }

    /// <summary>
    /// Rate limit header'larını ekler
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="clientId">Client ID</param>
    /// <param name="endpoint">Endpoint</param>
    /// <returns>Task</returns>
    private async Task AddRateLimitHeadersAsync(HttpContext context, string clientId, string endpoint)
    {
        var remaining = await _rateLimitService.GetRemainingRequestsAsync(clientId, endpoint);
        var resetTime = await _rateLimitService.GetResetTimeAsync(clientId, endpoint);

        context.Response.Headers["X-RateLimit-Limit"] = "10";
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = resetTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
} 