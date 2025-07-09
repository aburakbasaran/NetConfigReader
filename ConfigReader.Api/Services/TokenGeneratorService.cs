using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace ConfigReader.Api.Services;

/// <summary>
/// Token üretme service implementasyonu
/// </summary>
public sealed class TokenGeneratorService : ITokenGeneratorService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<TokenGeneratorService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _tokenTracker;
    private const string TokenPrefix = "generated_token_";
    private const string TrackerKey = "token_tracker";

    public TokenGeneratorService(
        IMemoryCache memoryCache,
        ILogger<TokenGeneratorService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenTracker = new ConcurrentDictionary<string, DateTime>();
    }

    /// <summary>
    /// Yeni token üretir ve cache'e kaydeder
    /// </summary>
    /// <param name="expiryMinutes">Token'ın geçerlilik süresi (dakika)</param>
    /// <returns>Üretilen token</returns>
    public Task<string> GenerateTokenAsync(int expiryMinutes = 1)
    {
        // Eski token'ları temizle (sadece aktif olanları - expired'ları zaten otomatik silinir)
        CleanupActiveTokens();

        var token = GenerateSecureToken();
        var expiryTime = DateTime.UtcNow.AddMinutes(expiryMinutes);
        
        var cacheKey = TokenPrefix + token;
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expiryTime,
            SlidingExpiration = TimeSpan.FromMinutes(Math.Max(expiryMinutes / 2, 0.5)), // Minimum 30 saniye
            Priority = CacheItemPriority.High
        };

        _memoryCache.Set(cacheKey, new TokenInfo
        {
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiryTime,
            IsActive = true
        }, cacheOptions);

        // Token tracker'a ekle
        _tokenTracker.TryAdd(token, expiryTime);

        _logger.LogInformation("Token üretildi: {Token}, Expiry: {Expiry}", 
            token, expiryTime.ToString("yyyy-MM-dd HH:mm:ss"));

        return Task.FromResult(token);
    }

    /// <summary>
    /// Aktif token'ları temizler (yeni token üretilirken eski aktif token'ları siler)
    /// </summary>
    private void CleanupActiveTokens()
    {
        var now = DateTime.UtcNow;
        var tokensToRemove = new List<string>();

        foreach (var kvp in _tokenTracker)
        {
            var token = kvp.Key;
            var expiryTime = kvp.Value;

            // Süresi dolmuş token'ları ve aktif token'ları temizle
            var cacheKey = TokenPrefix + token;
            if (_memoryCache.TryGetValue(cacheKey, out TokenInfo? tokenInfo) && tokenInfo != null)
            {
                // Yeni token üretilirken tüm eski token'ları sil
                tokensToRemove.Add(token);
            }
        }

        // Token'ları sil
        foreach (var token in tokensToRemove)
        {
            var cacheKey = TokenPrefix + token;
            _memoryCache.Remove(cacheKey);
            _tokenTracker.TryRemove(token, out _);
        }

        if (tokensToRemove.Count > 0)
        {
            _logger.LogInformation("Temizlenen token sayısı: {Count}", tokensToRemove.Count);
        }
    }

    /// <summary>
    /// Token'ın geçerli olup olmadığını kontrol eder
    /// </summary>
    /// <param name="token">Kontrol edilecek token</param>
    /// <returns>Token geçerli ise true</returns>
    public Task<bool> IsTokenValidAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(false);
        }

        var cacheKey = TokenPrefix + token;
        
        if (_memoryCache.TryGetValue(cacheKey, out TokenInfo? tokenInfo))
        {
            var isValid = tokenInfo != null && 
                         tokenInfo.IsActive && 
                         tokenInfo.ExpiresAt > DateTime.UtcNow;

            if (isValid)
            {
                _logger.LogDebug("Token geçerli: {Token}", token);
            }
            else
            {
                _logger.LogDebug("Token geçersiz veya süresi dolmuş: {Token}", token);
                // Cache'den kaldır
                _memoryCache.Remove(cacheKey);
                _tokenTracker.TryRemove(token, out _);
            }

            return Task.FromResult(isValid);
        }

        _logger.LogDebug("Token cache'de bulunamadı: {Token}", token);
        return Task.FromResult(false);
    }

    /// <summary>
    /// Token'ı cache'den kaldırır
    /// </summary>
    /// <param name="token">Kaldırılacak token</param>
    /// <returns>Task</returns>
    public Task RevokeTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.CompletedTask;
        }

        var cacheKey = TokenPrefix + token;
        _memoryCache.Remove(cacheKey);
        _tokenTracker.TryRemove(token, out _);

        _logger.LogInformation("Token iptal edildi: {Token}", token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tüm aktif token'ları listeler
    /// </summary>
    /// <returns>Aktif token listesi</returns>
    public Task<List<string>> GetActiveTokensAsync()
    {
        var activeTokens = new List<string>();
        var now = DateTime.UtcNow;

        foreach (var kvp in _tokenTracker)
        {
            var token = kvp.Key;
            var expiryTime = kvp.Value;

            if (expiryTime > now)
            {
                var cacheKey = TokenPrefix + token;
                if (_memoryCache.TryGetValue(cacheKey, out TokenInfo? tokenInfo) && 
                    tokenInfo != null && tokenInfo.IsActive)
                {
                    activeTokens.Add(token);
                }
            }
            else
            {
                // Süresi dolmuş token'ı temizle
                _tokenTracker.TryRemove(token, out _);
            }
        }

        return Task.FromResult(activeTokens);
    }

    /// <summary>
    /// Güvenli token üretir
    /// </summary>
    /// <returns>Üretilen token</returns>
    private static string GenerateSecureToken()
    {
        var guid = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        
        return $"tk_{guid}_{timestamp}";
    }

    /// <summary>
    /// Token bilgilerini içeren class
    /// </summary>
    private sealed class TokenInfo
    {
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }
} 