namespace ConfigReader.Api.Services;

/// <summary>
/// Data masking için service interface
/// </summary>
public interface IDataMaskingService
{
    /// <summary>
    /// Production ortamında data'yı maskeler
    /// </summary>
    /// <param name="value">Maskelenecek değer</param>
    /// <returns>Maskelenmiş değer</returns>
    string MaskValue(string? value);

    /// <summary>
    /// Production ortamında mı kontrol eder
    /// </summary>
    /// <returns>Production ise true</returns>
    bool IsProductionEnvironment();

    /// <summary>
    /// Value'nun maskelenmesi gerekip gerekmediğini kontrol eder
    /// </summary>
    /// <param name="value">Kontrol edilecek değer</param>
    /// <returns>Maskelenmesi gerekiyorsa true</returns>
    bool ShouldMaskValue(string? value);
} 