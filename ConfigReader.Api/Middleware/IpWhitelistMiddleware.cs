using System.Net;
using System.Text.Json;
using ConfigReader.Api.Services;

namespace ConfigReader.Api.Middleware;

/// <summary>
/// IP whitelist kontrolü yapan middleware
/// </summary>
public class IpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIpWhitelistService _ipWhitelistService;
    private readonly ILogger<IpWhitelistMiddleware> _logger;

    public IpWhitelistMiddleware(
        RequestDelegate next,
        IIpWhitelistService ipWhitelistService,
        ILogger<IpWhitelistMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _ipWhitelistService = ipWhitelistService ?? throw new ArgumentNullException(nameof(ipWhitelistService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_ipWhitelistService.IsWhitelistEnabled())
        {
            await _next(context);
            return;
        }

        var clientIp = GetClientIpAddress(context);
        
        if (string.IsNullOrEmpty(clientIp))
        {
            _logger.LogWarning("Could not determine client IP address");
            await WriteUnauthorizedResponse(context, "Unable to determine client IP address");
            return;
        }

        if (!_ipWhitelistService.IsIpAllowed(clientIp))
        {
            _logger.LogWarning("IP address {ClientIp} is not in whitelist, blocking request to {Path}", clientIp, context.Request.Path);
            await WriteUnauthorizedResponse(context, "Access denied: IP address not in whitelist");
            return;
        }

        _logger.LogDebug("IP address {ClientIp} is whitelisted, allowing request to {Path}", clientIp, context.Request.Path);
        await _next(context);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // X-Forwarded-For header'ını kontrol et (proxy/load balancer durumları için)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For birden fazla IP içerebilir, ilkini al
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out _))
            {
                return firstIp;
            }
        }

        // X-Real-IP header'ını kontrol et
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp) && IPAddress.TryParse(realIp, out _))
        {
            return realIp;
        }

        // Remote IP address'i al
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            // IPv6 loopback'i IPv4'e çevir
            if (remoteIp.IsIPv4MappedToIPv6)
            {
                remoteIp = remoteIp.MapToIPv4();
            }

            return remoteIp.ToString();
        }

        return string.Empty;
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Access Denied",
            message = message,
            statusCode = 403,
            timestamp = DateTime.UtcNow
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
} 