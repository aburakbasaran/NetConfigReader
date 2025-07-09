namespace ConfigReader.Api.Services;

/// <summary>
/// Token authentication için service interface
/// </summary>
public interface ITokenAuthenticationService
{
    /// <summary>
    /// Token'ı validate eder
    /// </summary>
    /// <param name="token">Validate edilecek token</param>
    /// <returns>Token geçerli ise true</returns>
    bool ValidateToken(string? token);

    /// <summary>
    /// Token'ın formatını kontrol eder
    /// </summary>
    /// <param name="token">Kontrol edilecek token</param>
    /// <returns>Format geçerli ise true</returns>
    bool IsValidTokenFormat(string? token);

    /// <summary>
    /// Token'ı hash'ler
    /// </summary>
    /// <param name="token">Hash'lenecek token</param>
    /// <returns>Hash'lenmiş token</returns>
    string HashToken(string token);

    /// <summary>
    /// Authentication gerekli mi kontrol eder
    /// </summary>
    /// <returns>Authentication gerekli ise true</returns>
    bool IsAuthenticationRequired();
} 