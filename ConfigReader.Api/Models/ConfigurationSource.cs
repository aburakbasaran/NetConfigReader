namespace ConfigReader.Api.Models;

/// <summary>
/// Configuration kaynağı türleri
/// </summary>
public enum ConfigurationSource
{
    /// <summary>
    /// Environment değişkenleri
    /// </summary>
    Environment,
    
    /// <summary>
    /// AppSettings dosyaları
    /// </summary>
    AppSettings
}

/// <summary>
/// Configuration source helper metotları
/// </summary>
public static class ConfigurationSourceExtensions
{
    /// <summary>
    /// Enum'u string'e çevirir
    /// </summary>
    /// <param name="source">Configuration source</param>
    /// <returns>String değeri</returns>
    public static string ToStringValue(this ConfigurationSource source) => source.ToString();
} 