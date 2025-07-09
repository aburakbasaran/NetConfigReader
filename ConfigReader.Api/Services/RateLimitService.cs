using System.Collections.Concurrent;

namespace ConfigReader.Api.Services;

/// <summary>
/// Memory-based rate limiting implementation
/// </summary>
public sealed class RateLimitService : IRateLimitService
{
    private readonly ILogger<RateLimitService> _logger;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore;
    private readonly Timer _cleanupTimer;

    /// <summary>
    /// Günlük maksimum istek sayısı
    /// </summary>
    private const int DailyRequestLimit = 10;

    public RateLimitService(ILogger<RateLimitService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rateLimitStore = new ConcurrentDictionary<string, RateLimitInfo>();
        
        // Her saat cleanup yapmak için timer
        _cleanupTimer = new Timer(Cleanup, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }

    /// <summary>
    /// İstek yapılabilir mi kontrol eder
    /// </summary>
    /// <param name="clientId">Client identifier (IP, user ID, etc.)</param>
    /// <param name="endpoint">API endpoint</param>
    /// <returns>İstek yapılabilir ise true</returns>
    public Task<bool> IsRequestAllowedAsync(string clientId, string endpoint)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(clientId))
        {
            _logger.LogDebug("Empty clientId provided, allowing request");
            return Task.FromResult(true);
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _logger.LogDebug("Empty endpoint provided, allowing request");
            return Task.FromResult(true);
        }

        var key = GenerateKey(clientId, endpoint);
        var now = DateTime.UtcNow;
        var today = now.Date;

        var rateLimitInfo = _rateLimitStore.AddOrUpdate(key, 
            // Yeni entry oluştur
            new RateLimitInfo 
            { 
                RequestCount = 1, 
                ResetDate = today.AddDays(1), 
                LastRequestTime = now 
            },
            // Mevcut entry'yi güncelle
            (_, existing) =>
            {
                // Yeni gün başladıysa reset et
                if (existing.ResetDate <= now)
                {
                    existing.RequestCount = 1;
                    existing.ResetDate = today.AddDays(1);
                    existing.LastRequestTime = now;
                }
                else
                {
                    existing.RequestCount++;
                    existing.LastRequestTime = now;
                }
                return existing;
            });

        var isAllowed = rateLimitInfo.RequestCount <= DailyRequestLimit;
        
        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}. Count: {Count}",
                clientId, endpoint, rateLimitInfo.RequestCount);
        }
        else
        {
            _logger.LogDebug("Request allowed for client {ClientId} on endpoint {Endpoint}. Count: {Count}/{Limit}",
                clientId, endpoint, rateLimitInfo.RequestCount, DailyRequestLimit);
        }

        return Task.FromResult(isAllowed);
    }

    /// <summary>
    /// Kalan istek sayısını getirir
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <param name="endpoint">API endpoint</param>
    /// <returns>Kalan istek sayısı</returns>
    public Task<int> GetRemainingRequestsAsync(string clientId, string endpoint)
    {
        var key = GenerateKey(clientId, endpoint);
        var now = DateTime.UtcNow;
        
        if (_rateLimitStore.TryGetValue(key, out var rateLimitInfo))
        {
            // Yeni gün başladıysa reset et
            if (rateLimitInfo.ResetDate <= now)
            {
                return Task.FromResult(DailyRequestLimit);
            }
            
            var remaining = Math.Max(0, DailyRequestLimit - rateLimitInfo.RequestCount);
            return Task.FromResult(remaining);
        }

        return Task.FromResult(DailyRequestLimit);
    }

    /// <summary>
    /// Rate limit reset süresini getirir
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <param name="endpoint">API endpoint</param>
    /// <returns>Reset süresi (DateTime)</returns>
    public Task<DateTime> GetResetTimeAsync(string clientId, string endpoint)
    {
        var key = GenerateKey(clientId, endpoint);
        var now = DateTime.UtcNow;
        
        if (_rateLimitStore.TryGetValue(key, out var rateLimitInfo))
        {
            // Yeni gün başladıysa bugünün sonu
            if (rateLimitInfo.ResetDate <= now)
            {
                return Task.FromResult(now.Date.AddDays(1));
            }
            
            return Task.FromResult(rateLimitInfo.ResetDate);
        }

        return Task.FromResult(now.Date.AddDays(1));
    }

    /// <summary>
    /// Rate limit key oluşturur
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <param name="endpoint">API endpoint</param>
    /// <returns>Unique key</returns>
    private static string GenerateKey(string clientId, string endpoint)
    {
        return $"{clientId}:{endpoint}";
    }

    /// <summary>
    /// Eski rate limit entry'lerini temizler
    /// </summary>
    /// <param name="state">Timer state</param>
    private void Cleanup(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var cutoffDate = now.AddDays(-2);
            var cleanupCount = 0;

            // Thread-safe cleanup: Take snapshot and remove stale entries
            var keysToRemove = _rateLimitStore
                .Where(kvp => kvp.Value.ResetDate < cutoffDate)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                if (_rateLimitStore.TryRemove(key, out _))
                {
                    cleanupCount++;
                }
            }

            if (cleanupCount > 0)
            {
                _logger.LogDebug("Cleaned up {Count} old rate limit entries", cleanupCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rate limit cleanup");
        }
    }

    /// <summary>
    /// Resource'ları temizler
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

/// <summary>
/// Rate limit bilgilerini tutar
/// </summary>
internal sealed class RateLimitInfo
{
    public int RequestCount { get; set; }
    public DateTime ResetDate { get; set; }
    public DateTime LastRequestTime { get; set; }
} 