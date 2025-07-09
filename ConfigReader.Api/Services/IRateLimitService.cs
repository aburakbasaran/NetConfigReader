namespace ConfigReader.Api.Services;

/// <summary>
/// Rate limiting için service interface
/// </summary>
public interface IRateLimitService : IDisposable
{
    /// <summary>
    /// İstek yapılabilir mi kontrol eder
    /// </summary>
    /// <param name="clientId">Client identifier (IP, user ID, etc.)</param>
    /// <param name="endpoint">API endpoint</param>
    /// <returns>İstek yapılabilir ise true</returns>
    Task<bool> IsRequestAllowedAsync(string clientId, string endpoint);

    /// <summary>
    /// Kalan istek sayısını getirir
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <param name="endpoint">API endpoint</param>
    /// <returns>Kalan istek sayısı</returns>
    Task<int> GetRemainingRequestsAsync(string clientId, string endpoint);

    /// <summary>
    /// Rate limit reset süresini getirir
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <param name="endpoint">API endpoint</param>
    /// <returns>Reset süresi (DateTime)</returns>
    Task<DateTime> GetResetTimeAsync(string clientId, string endpoint);
} 