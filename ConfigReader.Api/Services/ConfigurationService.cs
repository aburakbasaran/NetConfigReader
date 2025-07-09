using ConfigReader.Api.Models;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace ConfigReader.Api.Services;

/// <summary>
/// Configuration değerlerini okumak için service implementation
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(IConfiguration configuration, ILogger<ConfigurationService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm configuration değerlerini getirir
    /// </summary>
    /// <returns>Configuration değerleri listesi</returns>
    public async Task<IEnumerable<ConfigurationItem>> GetAllConfigurationsAsync()
    {
        var configItems = new List<ConfigurationItem>();

        try
        {
            // Environment değişkenlerini ekle
            var envVars = await GetEnvironmentVariablesAsync().ConfigureAwait(false);
            configItems.AddRange(envVars);

            // AppSettings değerlerini ekle
            var appSettings = await GetAppSettingsAsync().ConfigureAwait(false);
            configItems.AddRange(appSettings);

            _logger.LogInformation("Toplam {Count} configuration değeri getirildi.", configItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration değerleri getirilirken hata oluştu.");
            throw;
        }

        return configItems;
    }

    /// <summary>
    /// Belirli bir anahtarın değerini getirir
    /// </summary>
    /// <param name="key">Configuration anahtarı</param>
    /// <returns>Configuration değeri</returns>
    public Task<ConfigurationItem?> GetConfigurationAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Configuration anahtarı boş veya null.");
            return Task.FromResult<ConfigurationItem?>(null);
        }

        try
        {
            var value = _configuration[key];
            
            if (value is null)
            {
                _logger.LogInformation("'{Key}' anahtarı için değer bulunamadı.", key);
                return Task.FromResult<ConfigurationItem?>(null);
            }

            // Kaynağı belirle
            var source = DetermineSource(key);
            
            var result = new ConfigurationItem
            {
                Key = key,
                Value = value,
                Source = source
            };

            return Task.FromResult<ConfigurationItem?>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "'{Key}' anahtarı için değer getirilirken hata oluştu.", key);
            throw;
        }
    }

    /// <summary>
    /// Sadece environment değişkenlerini getirir
    /// </summary>
    /// <returns>Environment değişkenleri listesi</returns>
    public Task<IEnumerable<ConfigurationItem>> GetEnvironmentVariablesAsync()
    {
        try
        {
            var environmentVariables = Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .Where(kvp => !string.IsNullOrEmpty(kvp.Key?.ToString()))
                .Select(kvp => new ConfigurationItem
                {
                    Key = kvp.Key!.ToString()!,
                    Value = kvp.Value?.ToString(),
                    Source = ConfigurationSource.Environment
                })
                .ToList();

            _logger.LogInformation("{Count} environment değişkeni getirildi.", environmentVariables.Count);
            return Task.FromResult<IEnumerable<ConfigurationItem>>(environmentVariables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Environment değişkenleri getirilirken hata oluştu.");
            throw;
        }
    }

    /// <summary>
    /// Sadece appsettings değerlerini getirir
    /// </summary>
    /// <returns>AppSettings değerleri listesi</returns>
    public Task<IEnumerable<ConfigurationItem>> GetAppSettingsAsync()
    {
        try
        {
            var appSettings = ExtractAppSettingsFromConfiguration();
            _logger.LogInformation("{Count} appsettings değeri getirildi.", appSettings.Count);
            return Task.FromResult<IEnumerable<ConfigurationItem>>(appSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AppSettings değerleri getirilirken hata oluştu.");
            throw;
        }
    }

    /// <summary>
    /// Configuration'dan AppSettings değerlerini çıkarır
    /// </summary>
    /// <returns>AppSettings değerleri</returns>
    private List<ConfigurationItem> ExtractAppSettingsFromConfiguration()
    {
        var appSettings = new List<ConfigurationItem>();

        if (_configuration is not IConfigurationRoot configurationRoot)
        {
            return appSettings;
        }

        foreach (var provider in configurationRoot.Providers.Reverse())
        {
            var keys = GetAllKeysFromProvider(provider);
            
            foreach (var key in keys)
            {
                if (provider.TryGet(key, out var value) && 
                    !IsEnvironmentVariable(key) && 
                    !appSettings.Any(x => x.Key == key))
                {
                    appSettings.Add(new ConfigurationItem
                    {
                        Key = key,
                        Value = value,
                        Source = ConfigurationSource.AppSettings
                    });
                }
            }
        }

        return appSettings;
    }

    /// <summary>
    /// Configuration anahtarının kaynağını belirler
    /// </summary>
    /// <param name="key">Configuration anahtarı</param>
    /// <returns>Kaynak türü</returns>
    private ConfigurationSource DetermineSource(string key)
    {
        return IsEnvironmentVariable(key) 
            ? ConfigurationSource.Environment 
            : ConfigurationSource.AppSettings;
    }

    /// <summary>
    /// Bir anahtarın environment değişkeni olup olmadığını kontrol eder
    /// </summary>
    /// <param name="key">Configuration anahtarı</param>
    /// <returns>Environment değişkeni ise true</returns>
    private static bool IsEnvironmentVariable(string key)
    {
        return Environment.GetEnvironmentVariable(key) is not null;
    }

    /// <summary>
    /// Configuration provider'dan tüm anahtarları alır
    /// </summary>
    /// <param name="provider">Configuration provider</param>
    /// <returns>Anahtar listesi</returns>
    private static IEnumerable<string> GetAllKeysFromProvider(IConfigurationProvider provider)
    {
        var keys = new List<string>();
        
        try
        {
            // Reflection kullanarak provider'dan tüm anahtarları al
            var type = provider.GetType();
            var dataProperty = type.GetProperty("Data", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (dataProperty?.GetValue(provider) is IDictionary<string, string> data)
            {
                keys.AddRange(data.Keys);
            }
        }
        catch (Exception)
        {
            // Reflection hatalarını sessizce yok say
        }

        return keys;
    }
} 