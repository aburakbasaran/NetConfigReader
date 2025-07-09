using ConfigReader.Api.Models;
using ConfigReader.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConfigReader.Api.Controllers;

/// <summary>
/// Configuration değerlerini yönetmek için controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ConfigurationController : ControllerBase
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(IConfigurationService configurationService, ILogger<ConfigurationController> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm configuration değerlerini getirir
    /// </summary>
    /// <returns>Configuration değerleri listesi</returns>
    /// <response code="200">Configuration değerleri başarıyla getirildi</response>
    /// <response code="500">Sunucu hatası</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ConfigurationItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ConfigurationItem>>> GetAllConfigurations()
    {
        try
        {
            _logger.LogInformation("Tüm configuration değerleri istendi.");
            var configurations = await _configurationService.GetAllConfigurationsAsync().ConfigureAwait(false);
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration değerleri getirilirken hata oluştu.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Configuration değerleri getirilemedi.");
        }
    }

    /// <summary>
    /// Belirli bir anahtarın configuration değerini getirir
    /// </summary>
    /// <param name="key">Configuration anahtarı</param>
    /// <returns>Configuration değeri</returns>
    /// <response code="200">Configuration değeri başarıyla getirildi</response>
    /// <response code="404">Anahtar bulunamadı</response>
    /// <response code="400">Geçersiz anahtar</response>
    /// <response code="500">Sunucu hatası</response>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(ConfigurationItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ConfigurationItem>> GetConfiguration(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Geçersiz configuration anahtarı: {Key}", key);
            return BadRequest("Configuration anahtarı geçersiz.");
        }

        try
        {
            _logger.LogInformation("Configuration değeri istendi: {Key}", key);
            var configuration = await _configurationService.GetConfigurationAsync(key).ConfigureAwait(false);
            
            if (configuration is null)
            {
                _logger.LogInformation("Configuration anahtarı bulunamadı: {Key}", key);
                return NotFound($"'{key}' anahtarı için configuration değeri bulunamadı.");
            }

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration değeri getirilirken hata oluştu: {Key}", key);
            return StatusCode(StatusCodes.Status500InternalServerError, "Configuration değeri getirilemedi.");
        }
    }

    /// <summary>
    /// Sadece environment değişkenlerini getirir
    /// </summary>
    /// <returns>Environment değişkenleri listesi</returns>
    /// <response code="200">Environment değişkenleri başarıyla getirildi</response>
    /// <response code="500">Sunucu hatası</response>
    [HttpGet("environment")]
    [ProducesResponseType(typeof(IEnumerable<ConfigurationItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ConfigurationItem>>> GetEnvironmentVariables()
    {
        try
        {
            _logger.LogInformation("Environment değişkenleri istendi.");
            var environmentVariables = await _configurationService.GetEnvironmentVariablesAsync().ConfigureAwait(false);
            return Ok(environmentVariables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Environment değişkenleri getirilirken hata oluştu.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Environment değişkenleri getirilemedi.");
        }
    }

    /// <summary>
    /// Sadece appsettings değerlerini getirir
    /// </summary>
    /// <returns>AppSettings değerleri listesi</returns>
    /// <response code="200">AppSettings değerleri başarıyla getirildi</response>
    /// <response code="500">Sunucu hatası</response>
    [HttpGet("appsettings")]
    [ProducesResponseType(typeof(IEnumerable<ConfigurationItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ConfigurationItem>>> GetAppSettings()
    {
        try
        {
            _logger.LogInformation("AppSettings değerleri istendi.");
            var appSettings = await _configurationService.GetAppSettingsAsync().ConfigureAwait(false);
            return Ok(appSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AppSettings değerleri getirilirken hata oluştu.");
            return StatusCode(StatusCodes.Status500InternalServerError, "AppSettings değerleri getirilemedi.");
        }
    }
} 