using ConfigReader.Api.Models;

namespace ConfigReader.Api.Services;

/// <summary>
/// Configuration değerlerini okumak için service interface
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Tüm configuration değerlerini getirir
    /// </summary>
    /// <returns>Configuration değerleri listesi</returns>
    Task<IEnumerable<ConfigurationItem>> GetAllConfigurationsAsync();

    /// <summary>
    /// Belirli bir anahtarın değerini getirir
    /// </summary>
    /// <param name="key">Configuration anahtarı</param>
    /// <returns>Configuration değeri</returns>
    Task<ConfigurationItem?> GetConfigurationAsync(string key);

    /// <summary>
    /// Sadece environment değişkenlerini getirir
    /// </summary>
    /// <returns>Environment değişkenleri listesi</returns>
    Task<IEnumerable<ConfigurationItem>> GetEnvironmentVariablesAsync();

    /// <summary>
    /// Sadece appsettings değerlerini getirir
    /// </summary>
    /// <returns>AppSettings değerleri listesi</returns>
    Task<IEnumerable<ConfigurationItem>> GetAppSettingsAsync();
} 