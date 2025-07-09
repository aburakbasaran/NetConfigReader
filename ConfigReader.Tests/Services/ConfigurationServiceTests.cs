using ConfigReader.Api.Models;
using ConfigReader.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ConfigReader.Tests.Services;

/// <summary>
/// ConfigurationService unit testleri
/// </summary>
public sealed class ConfigurationServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<ConfigurationService>> _loggerMock;
    private readonly Mock<IDataMaskingService> _dataMaskingServiceMock;
    private readonly ConfigurationService _configurationService;

    public ConfigurationServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<ConfigurationService>>();
        _dataMaskingServiceMock = new Mock<IDataMaskingService>();
        
        // Default masking behavior - return value as is
        _dataMaskingServiceMock.Setup(x => x.MaskValue(It.IsAny<string?>()))
            .Returns((string? value) => value ?? string.Empty);
        
        _configurationService = new ConfigurationService(_configurationMock.Object, _loggerMock.Object, _dataMaskingServiceMock.Object);
    }

    [Fact]
    public async Task GetConfigurationAsync_ValidKey_ReturnsConfigurationItem()
    {
        // Arrange
        var key = "TestKey";
        var expectedValue = "TestValue";
        
        _configurationMock.Setup(x => x[key]).Returns(expectedValue);
        
        // Act
        var result = await _configurationService.GetConfigurationAsync(key);
        
        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be(key);
        result.Value.Should().Be(expectedValue);
        result.Source.Should().Be(ConfigurationSource.AppSettings);
    }

    [Fact]
    public async Task GetConfigurationAsync_InvalidKey_ReturnsNull()
    {
        // Arrange
        var key = "NonExistentKey";
        
        _configurationMock.Setup(x => x[key]).Returns((string?)null);
        
        // Act
        var result = await _configurationService.GetConfigurationAsync(key);
        
        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetConfigurationAsync_EmptyOrNullKey_ReturnsNull(string key)
    {
        // Act
        var result = await _configurationService.GetConfigurationAsync(key);
        
        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEnvironmentVariablesAsync_ReturnsEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TEST_ENV_VAR", "TestValue");
        
        // Act
        var result = await _configurationService.GetEnvironmentVariablesAsync();
        
        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(x => x.Key == "TEST_ENV_VAR" && x.Value == "TestValue" && x.Source == ConfigurationSource.Environment);
        
        // Cleanup
        Environment.SetEnvironmentVariable("TEST_ENV_VAR", null);
    }

    [Fact]
    public async Task GetAppSettingsAsync_ReturnsAppSettingsValues()
    {
        // Arrange
        var configurationRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"TestKey1", "TestValue1"},
                {"TestKey2", "TestValue2"}
            })
            .Build();

        var service = new ConfigurationService(configurationRoot, _loggerMock.Object, _dataMaskingServiceMock.Object);
        
        // Act
        var result = await service.GetAppSettingsAsync();
        
        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(x => x.Key == "TestKey1" && x.Value == "TestValue1" && x.Source == ConfigurationSource.AppSettings);
        result.Should().Contain(x => x.Key == "TestKey2" && x.Value == "TestValue2" && x.Source == ConfigurationSource.AppSettings);
    }

    [Fact]
    public async Task GetAllConfigurationsAsync_ReturnsAllConfigurations()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TEST_ENV_VAR", "EnvValue");
        
        var configurationRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"TestAppKey", "AppValue"}
            })
            .Build();

        var service = new ConfigurationService(configurationRoot, _loggerMock.Object, _dataMaskingServiceMock.Object);
        
        // Act
        var result = await service.GetAllConfigurationsAsync();
        
        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(x => x.Key == "TEST_ENV_VAR" && x.Source == ConfigurationSource.Environment);
        result.Should().Contain(x => x.Key == "TestAppKey" && x.Source == ConfigurationSource.AppSettings);
        
        // Cleanup
        Environment.SetEnvironmentVariable("TEST_ENV_VAR", null);
    }

    [Fact]
    public async Task GetConfigurationAsync_EnvironmentVariable_ReturnsEnvironmentSource()
    {
        // Arrange
        var key = "TEST_ENV_KEY";
        var value = "TestEnvValue";
        
        Environment.SetEnvironmentVariable(key, value);
        _configurationMock.Setup(x => x[key]).Returns(value);
        
        // Act
        var result = await _configurationService.GetConfigurationAsync(key);
        
        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be(key);
        result.Value.Should().Be(value);
        result.Source.Should().Be(ConfigurationSource.Environment);
        
        // Cleanup
        Environment.SetEnvironmentVariable(key, null);
    }

    [Fact]
    public async Task GetEnvironmentVariablesAsync_HandlesEmptyEnvironment()
    {
        // Act
        var result = await _configurationService.GetEnvironmentVariablesAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<ConfigurationItem>>();
    }

    [Fact]
    public async Task GetAppSettingsAsync_HandlesEmptyConfiguration()
    {
        // Arrange
        var emptyConfiguration = new ConfigurationBuilder().Build();
        var service = new ConfigurationService(emptyConfiguration, _loggerMock.Object, _dataMaskingServiceMock.Object);
        
        // Act
        var result = await service.GetAppSettingsAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ConfigurationService_Constructor_WithNullArguments_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var configurationAction = () => new ConfigurationService(null!, _loggerMock.Object, _dataMaskingServiceMock.Object);
        configurationAction.Should().Throw<ArgumentNullException>();
        
        var loggerAction = () => new ConfigurationService(_configurationMock.Object, null!, _dataMaskingServiceMock.Object);
        loggerAction.Should().Throw<ArgumentNullException>();
        
        var dataMaskingAction = () => new ConfigurationService(_configurationMock.Object, _loggerMock.Object, null!);
        dataMaskingAction.Should().Throw<ArgumentNullException>();
    }
} 