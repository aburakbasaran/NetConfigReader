namespace ConfigReader.Api.Models;

/// <summary>
/// Configuration değerlerini temsil eden model
/// </summary>
public sealed class ConfigurationItem
{
    /// <summary>
    /// Configuration anahtarı
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Configuration değeri
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Configuration kaynağı
    /// </summary>
    public required ConfigurationSource Source { get; init; }

    /// <summary>
    /// String formatında kaynak bilgisi
    /// </summary>
    public string SourceName => Source.ToStringValue();
} 