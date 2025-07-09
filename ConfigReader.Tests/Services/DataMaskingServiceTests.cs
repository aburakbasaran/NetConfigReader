using ConfigReader.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace ConfigReader.Tests.Services;

/// <summary>
/// DataMaskingService unit testleri
/// </summary>
public sealed class DataMaskingServiceTests
{
    private readonly Mock<ILogger<DataMaskingService>> _loggerMock;
    private readonly Mock<IWebHostEnvironment> _hostEnvironmentMock;

    public DataMaskingServiceTests()
    {
        _loggerMock = new Mock<ILogger<DataMaskingService>>();
        _hostEnvironmentMock = new Mock<IWebHostEnvironment>();
    }

    [Fact]
    public void MaskValue_DevelopmentEnvironment_ReturnsOriginalValue()
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);
        var originalValue = "MySensitiveData";

        // Act
        var result = service.MaskValue(originalValue);

        // Assert
        result.Should().Be(originalValue);
    }

    [Fact]
    public void MaskValue_ProductionEnvironment_ShortValue_ReturnsOriginalValue()
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);
        var shortValue = "short";

        // Act
        var result = service.MaskValue(shortValue);

        // Assert
        result.Should().Be(shortValue);
    }

    [Fact]
    public void MaskValue_ProductionEnvironment_LongValue_ReturnsMaskedValue()
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);
        var longValue = "ThisIsAVeryLongSensitiveValue";

        // Act
        var result = service.MaskValue(longValue);

        // Assert
        result.Should().Be("ThisI...Value");
    }

    [Fact]
    public void MaskValue_ProductionEnvironment_ExactlyTenCharacters_ReturnsOriginalValue()
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);
        var tenCharValue = "1234567890";

        // Act
        var result = service.MaskValue(tenCharValue);

        // Assert
        result.Should().Be(tenCharValue);
    }

    [Fact]
    public void MaskValue_ProductionEnvironment_ElevenCharacters_ReturnsMaskedValue()
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);
        var elevenCharValue = "12345678901";

        // Act
        var result = service.MaskValue(elevenCharValue);

        // Assert
        result.Should().Be("12345...78901");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MaskValue_EmptyOrNullValue_ReturnsEmptyPlaceholder(string? value)
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);

        // Act
        var result = service.MaskValue(value);

        // Assert
        result.Should().Be("[EMPTY]");
    }

    [Fact]
    public void MaskValue_WhitespaceValue_ReturnsOriginalValue()
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);
        var whitespace = "   ";

        // Act
        var result = service.MaskValue(whitespace);

        // Assert - Whitespace değerler maskelenmez çünkü kısa ve özel karakter içerir
        result.Should().Be(whitespace);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("True")]
    [InlineData("FALSE")]
    public void MaskValue_BooleanValue_ReturnsOriginalValue(string booleanValue)
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);

        // Act
        var result = service.MaskValue(booleanValue);

        // Assert
        result.Should().Be(booleanValue);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("456.78")]
    [InlineData("-123")]
    [InlineData("0")]
    [InlineData("999.999")]
    public void MaskValue_NumericValue_ReturnsOriginalValue(string numericValue)
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);

        // Act
        var result = service.MaskValue(numericValue);

        // Assert
        result.Should().Be(numericValue);
    }

    [Theory]
    [InlineData("https://example.com/path")]
    [InlineData("http://localhost:8080")]
    [InlineData("ftp://files.example.com")]
    public void MaskValue_UrlValue_ReturnsMaskedValue(string urlValue)
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);

        // Act
        var result = service.MaskValue(urlValue);

        // Assert - URL'ler maskelendiği için first5...last5 formatında olmalı
        result.Should().StartWith(urlValue.Substring(0, 5));
        result.Should().EndWith(urlValue.Substring(urlValue.Length - 5));
        result.Should().Contain("...");
    }

    [Theory]
    [InlineData("/path/to/file")]
    [InlineData("C:\\Windows\\System32")]
    [InlineData("./relative/path")]
    [InlineData("../parent/directory")]
    public void MaskValue_PathValue_ReturnsMaskedValue(string pathValue)
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);

        // Act
        var result = service.MaskValue(pathValue);

        // Assert - Path'ler maskelendiği için first5...last5 formatında olmalı
        result.Should().StartWith(pathValue.Substring(0, 5));
        result.Should().EndWith(pathValue.Substring(pathValue.Length - 5));
        result.Should().Contain("...");
    }

    [Theory]
    [InlineData("Server=localhost;Database=test;User Id=sa;Password=secret;")]
    [InlineData("mongodb://username:password@localhost:27017/database")]
    [InlineData("redis://localhost:6379")]
    public void MaskValue_ConnectionString_ReturnsMaskedValue(string connectionString)
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);

        // Act
        var result = service.MaskValue(connectionString);

        // Assert - Connection string'ler maskelendiği için first5...last5 formatında olmalı
        result.Should().StartWith(connectionString.Substring(0, 5));
        result.Should().EndWith(connectionString.Substring(connectionString.Length - 5));
        result.Should().Contain("...");
    }

    [Fact]
    public void MaskValue_SensitiveValue_InProduction_GetsMasked()
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);
        var sensitiveValue = "MySecretApiKey123456789";

        // Act
        var result = service.MaskValue(sensitiveValue);

        // Assert
        result.Should().Be("MySec...56789");
    }

    [Fact]
    public void MaskValue_ProductionEnvironment_MasksLongValues()
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);
        var longValue = "ThisIsAVeryLongSensitiveValue";

        // Act
        var result = service.MaskValue(longValue);

        // Assert
        result.Should().Be("ThisI...Value");
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public void MaskValue_NonProductionEnvironment_DoesNotMask(string environment)
    {
        // Arrange
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns(environment);
        var service = new DataMaskingService(_loggerMock.Object, _hostEnvironmentMock.Object);
        var longValue = "ThisIsAVeryLongSensitiveValue";

        // Act
        var result = service.MaskValue(longValue);

        // Assert
        result.Should().Be(longValue); // Production değilse maskelenmiyor
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new DataMaskingService(null!, _hostEnvironmentMock.Object);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullHostEnvironment_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new DataMaskingService(_loggerMock.Object, null!);
        action.Should().Throw<ArgumentNullException>();
    }
} 