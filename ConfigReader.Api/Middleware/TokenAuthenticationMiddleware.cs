using ConfigReader.Api.Models;
using ConfigReader.Api.Services;
using Microsoft.Extensions.Options;
using System.Net;

namespace ConfigReader.Api.Middleware;

/// <summary>
/// Token authentication middleware
/// </summary>
public sealed class TokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenAuthenticationMiddleware> _logger;
    private readonly ITokenAuthenticationService _tokenAuthService;
    private readonly ConfigReaderApiOptions _options;

    /// <summary>
    /// Authentication gerekli endpoint'ler
    /// </summary>
    private static readonly string[] AuthenticatedEndpoints = 
    {
        "/api/configuration",
        "/api/configuration/environment",
        "/api/configuration/appsettings"
    };

    public TokenAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<TokenAuthenticationMiddleware> logger,
        ITokenAuthenticationService tokenAuthService,
        IOptions<ConfigReaderApiOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenAuthService = tokenAuthService ?? throw new ArgumentNullException(nameof(tokenAuthService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Middleware invoke metodu
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Authentication gerekli endpoint mi?
        if (!RequiresAuthentication(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Authentication gerekli mi?
        if (!_tokenAuthService.IsAuthenticationRequired())
        {
            _logger.LogDebug("Authentication disabled, skipping token validation");
            await _next(context);
            return;
        }

        try
        {
            // Token'ı header'dan al
            var token = ExtractTokenFromHeader(context);
            
            // Token validate et
            var isValid = _tokenAuthService.ValidateToken(token);
            
            if (!isValid)
            {
                await HandleUnauthorizedAsync(context);
                return;
            }

            // Token geçerli, devam et
            _logger.LogDebug("Token authentication successful for {Path}", context.Request.Path);
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token authentication error for {Path}", context.Request.Path);
            await HandleAuthenticationErrorAsync(context);
        }
    }

    /// <summary>
    /// Authentication gerekli mi kontrol eder
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>Authentication gerekli ise true</returns>
    private static bool RequiresAuthentication(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return AuthenticatedEndpoints.Any(endpoint => 
            path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Header'dan token'ı çıkarır
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Token veya null</returns>
    private string? ExtractTokenFromHeader(HttpContext context)
    {
        var headerName = _options.Security.TokenHeaderName;
        
        if (context.Request.Headers.TryGetValue(headerName, out var headerValue))
        {
            var token = headerValue.ToString().Trim();
            
            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("Token found in header {HeaderName}", headerName);
                return token;
            }
        }

        _logger.LogDebug("No token found in header {HeaderName}", headerName);
        return null;
    }

    /// <summary>
    /// Unauthorized response döner
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    private async Task HandleUnauthorizedAsync(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        
        _logger.LogWarning("Unauthorized access attempt from {ClientIp} to {Path} - UserAgent: {UserAgent}", 
            clientIp, context.Request.Path, userAgent);

        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        // Authentication header'ını set et
        context.Response.Headers["WWW-Authenticate"] = $"Bearer realm=\"ConfigReader API\"";

        var errorResponse = new
        {
            error = "Unauthorized",
            message = "Valid authentication token required",
            statusCode = 401,
            details = new
            {
                requiredHeader = _options.Security.TokenHeaderName,
                format = "Bearer <token>"
            },
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
    }

    /// <summary>
    /// Authentication error response döner
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    private async Task HandleAuthenticationErrorAsync(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);
        
        _logger.LogError("Authentication error for client {ClientIp} accessing {Path}", 
            clientIp, context.Request.Path);

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "Authentication Error",
            message = "An error occurred during authentication",
            statusCode = 500,
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