namespace ConfigReader.Api.Services;

/// <summary>
/// Production data masking implementation
/// </summary>
public sealed class DataMaskingService : IDataMaskingService
{
    private readonly ILogger<DataMaskingService> _logger;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Masking için sabit değerler
    /// </summary>
    private const int PrefixLength = 5;
    private const int SuffixLength = 5;
    private const string MaskString = "...";

    public DataMaskingService(ILogger<DataMaskingService> logger, IWebHostEnvironment environment)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Production ortamında data'yı maskeler
    /// </summary>
    /// <param name="value">Maskelenecek değer</param>
    /// <returns>Maskelenmiş değer</returns>
    public string MaskValue(string? value)
    {
        // Production ortamında değilse maskeleme yapma
        if (!IsProductionEnvironment())
        {
            return value ?? string.Empty;
        }

        // Null veya empty kontrolü
        if (string.IsNullOrEmpty(value))
        {
            return "[EMPTY]";
        }

        // Maskeleme gerekli mi kontrol et
        if (!ShouldMaskValue(value))
        {
            return value;
        }

        // Çok kısa değerler için tam maskeleme
        if (value.Length <= (PrefixLength + SuffixLength))
        {
            _logger.LogDebug("Value too short for partial masking, fully masking");
            return new string('*', Math.Min(value.Length, 10));
        }

        // Baştan 5 karakter + ... + sondan 5 karakter
        var prefix = value.Substring(0, PrefixLength);
        var suffix = value.Substring(value.Length - SuffixLength);
        var maskedValue = $"{prefix}{MaskString}{suffix}";

        _logger.LogDebug("Value masked in production environment");
        return maskedValue;
    }

    /// <summary>
    /// Production ortamında mı kontrol eder
    /// </summary>
    /// <returns>Production ise true</returns>
    public bool IsProductionEnvironment()
    {
        return _environment.IsProduction();
    }

    /// <summary>
    /// Value'nun maskelenmesi gerekip gerekmediğini kontrol eder
    /// </summary>
    /// <param name="value">Kontrol edilecek değer</param>
    /// <returns>Maskelenmesi gerekiyorsa true</returns>
    public bool ShouldMaskValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // Çok kısa değerler (örneğin "true", "false", "1", "0")
        if (value.Length <= 5)
        {
            // Boolean ve numeric değerler maskelenmesin
            if (IsBooleanOrNumericValue(value))
                return false;
        }

        // URL'ler maskelensin
        if (IsUrl(value))
            return true;

        // Connection string'ler maskelensin
        if (IsConnectionString(value))
            return true;

        // Path'ler maskelensin
        if (IsPath(value))
            return true;

        // Uzun değerler maskelensin (10 karakterden fazla)
        if (value.Length > 10)
            return true;

        return false;
    }

    /// <summary>
    /// Boolean veya numeric değer mi kontrol eder
    /// </summary>
    /// <param name="value">Kontrol edilecek değer</param>
    /// <returns>Boolean veya numeric ise true</returns>
    private static bool IsBooleanOrNumericValue(string value)
    {
        // Boolean kontrol
        if (bool.TryParse(value, out _))
            return true;

        // Numeric kontrol
        if (int.TryParse(value, out _) || double.TryParse(value, out _))
            return true;

        // Yaygın config değerleri
        var commonValues = new[] { "true", "false", "yes", "no", "on", "off", "1", "0" };
        return commonValues.Contains(value.ToLowerInvariant());
    }

    /// <summary>
    /// URL olup olmadığını kontrol eder
    /// </summary>
    /// <param name="value">Kontrol edilecek değer</param>
    /// <returns>URL ise true</returns>
    private static bool IsUrl(string value)
    {
        return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("jdbc:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Connection string olup olmadığını kontrol eder
    /// </summary>
    /// <param name="value">Kontrol edilecek değer</param>
    /// <returns>Connection string ise true</returns>
    private static bool IsConnectionString(string value)
    {
        var connectionKeywords = new[] { "server=", "database=", "user=", "password=", "host=", "port=" };
        var lowerValue = value.ToLowerInvariant();
        
        return connectionKeywords.Any(keyword => lowerValue.Contains(keyword));
    }

    /// <summary>
    /// Path olup olmadığını kontrol eder
    /// </summary>
    /// <param name="value">Kontrol edilecek değer</param>
    /// <returns>Path ise true</returns>
    private static bool IsPath(string value)
    {
        // Windows path
        if (value.Contains("\\") && (value.Contains("C:") || value.Contains("D:") || value.Contains("Program Files")))
            return true;

        // Unix path
        if (value.StartsWith("/") && value.Length > 5)
            return true;

        // Relative path
        if (value.Contains("./") || value.Contains("../"))
            return true;

        return false;
    }
} 