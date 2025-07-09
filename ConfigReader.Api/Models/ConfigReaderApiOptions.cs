namespace ConfigReader.Api.Models;

/// <summary>
/// ConfigReader API ayarları
/// </summary>
public sealed class ConfigReaderApiOptions
{
    /// <summary>
    /// Configuration section adı
    /// </summary>
    public const string SectionName = "ConfigReaderApi";

    /// <summary>
    /// API'nin aktif olup olmadığı
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// İzin verilen origin'ler
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Güvenlik ayarları
    /// </summary>
    public SecurityOptions Security { get; set; } = new();
}

/// <summary>
/// Güvenlik ayarları
/// </summary>
public sealed class SecurityOptions
{
    /// <summary>
    /// Authentication gerekli mi
    /// </summary>
    public bool RequireAuth { get; set; } = true;

    /// <summary>
    /// Rate limiting aktif mi
    /// </summary>
    public bool EnableRateLimit { get; set; } = true;

    /// <summary>
    /// Response masking aktif mi
    /// </summary>
    public bool EnableResponseMasking { get; set; } = true;

    /// <summary>
    /// API token'ları
    /// </summary>
    public string[] ApiTokens { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Token header adı
    /// </summary>
    public string TokenHeaderName { get; set; } = "X-ConfigReader-Token";
} 