namespace ConfigReader.Api.Services;

/// <summary>
/// Token üretme service interface
/// </summary>
public interface ITokenGeneratorService
{
    /// <summary>
    /// Yeni token üretir ve cache'e kaydeder
    /// </summary>
    /// <param name="expiryMinutes">Token'ın geçerlilik süresi (dakika)</param>
    /// <returns>Üretilen token</returns>
    Task<string> GenerateTokenAsync(int expiryMinutes = 60);

    /// <summary>
    /// Token'ın geçerli olup olmadığını kontrol eder
    /// </summary>
    /// <param name="token">Kontrol edilecek token</param>
    /// <returns>Token geçerli ise true</returns>
    Task<bool> IsTokenValidAsync(string token);

    /// <summary>
    /// Token'ı cache'den kaldırır
    /// </summary>
    /// <param name="token">Kaldırılacak token</param>
    /// <returns>Task</returns>
    Task RevokeTokenAsync(string token);

    /// <summary>
    /// Tüm aktif token'ları listeler
    /// </summary>
    /// <returns>Aktif token listesi</returns>
    Task<List<string>> GetActiveTokensAsync();
} 