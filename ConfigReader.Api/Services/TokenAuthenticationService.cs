using ConfigReader.Api.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ConfigReader.Api.Services;

/// <summary>
/// Token authentication service implementation
/// </summary>
public sealed class TokenAuthenticationService : ITokenAuthenticationService
{
    private readonly ILogger<TokenAuthenticationService> _logger;
    private readonly ConfigReaderApiOptions _options;
    private readonly string[] _hashedTokens;

    /// <summary>
    /// Minimum token uzunluğu
    /// </summary>
    private const int MinTokenLength = 32;

    /// <summary>
    /// Maximum token uzunluğu
    /// </summary>
    private const int MaxTokenLength = 512;

    /// <summary>
    /// Token format pattern
    /// </summary>
    private static readonly Regex TokenFormatPattern = new(@"^[A-Za-z0-9_\-]{32,512}$", RegexOptions.Compiled);

    public TokenAuthenticationService(
        ILogger<TokenAuthenticationService> logger,
        IOptions<ConfigReaderApiOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        // Validate configuration
        if (_options.Security?.ApiTokens == null)
        {
            _hashedTokens = Array.Empty<string>();
            _logger.LogWarning("No API tokens configured, all token validations will fail");
        }
        else
        {
            // Token'ları hash'le ve cache'le (startup'ta bir kez)
            _hashedTokens = _options.Security.ApiTokens
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Select(HashToken)
                .ToArray();
        }

        _logger.LogInformation("Token authentication service initialized with {Count} tokens", _hashedTokens.Length);
    }

    /// <summary>
    /// Token'ı validate eder
    /// </summary>
    /// <param name="token">Validate edilecek token</param>
    /// <returns>Token geçerli ise true</returns>
    public bool ValidateToken(string? token)
    {
        // Authentication gerekli değilse her zaman geçerli
        if (!IsAuthenticationRequired())
        {
            _logger.LogDebug("Authentication not required, token validation skipped");
            return true;
        }

        // Token null veya empty ise geçersiz
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogDebug("Token is null or empty");
            return false;
        }

        // Token format kontrolü
        if (!IsValidTokenFormat(token))
        {
            _logger.LogWarning("Invalid token format received");
            return false;
        }

        // Token'ı hash'le ve karşılaştır
        var hashedToken = HashToken(token);
        var isValid = _hashedTokens.Contains(hashedToken);

        if (isValid)
        {
            _logger.LogDebug("Token validation successful");
        }
        else
        {
            _logger.LogWarning("Token validation failed - invalid token");
        }

        return isValid;
    }

    /// <summary>
    /// Token'ın formatını kontrol eder
    /// </summary>
    /// <param name="token">Kontrol edilecek token</param>
    /// <returns>Format geçerli ise true</returns>
    public bool IsValidTokenFormat(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        // Uzunluk kontrolü
        if (token.Length < MinTokenLength || token.Length > MaxTokenLength)
        {
            _logger.LogDebug("Token length invalid: {Length} (min: {Min}, max: {Max})", 
                token.Length, MinTokenLength, MaxTokenLength);
            return false;
        }

        // Format pattern kontrolü
        if (!TokenFormatPattern.IsMatch(token))
        {
            _logger.LogDebug("Token format pattern invalid");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Token'ı hash'ler
    /// </summary>
    /// <param name="token">Hash'lenecek token</param>
    /// <returns>Hash'lenmiş token</returns>
    public string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be null or empty", nameof(token));

        // Salt ekleme - güvenlik için
        var saltedToken = $"ConfigReader_Salt_{token}_2024";
        
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedToken));
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// Authentication gerekli mi kontrol eder
    /// </summary>
    /// <returns>Authentication gerekli ise true</returns>
    public bool IsAuthenticationRequired()
    {
        return _options.Security.RequireAuth;
    }
} 