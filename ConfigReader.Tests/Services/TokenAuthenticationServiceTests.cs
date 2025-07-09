using ConfigReader.Api.Models;
using ConfigReader.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ConfigReader.Tests.Services;

/// <summary>
/// TokenAuthenticationService unit testleri
/// </summary>
public sealed class TokenAuthenticationServiceTests : IDisposable
{
    private readonly Mock<ILogger<TokenAuthenticationService>> _loggerMock;
    private readonly Mock<IOptions<ConfigReaderApiOptions>> _optionsMock;
    private readonly IMemoryCache _memoryCache;
    private readonly ConfigReaderApiOptions _options;

    public TokenAuthenticationServiceTests()
    {
        _loggerMock = new Mock<ILogger<TokenAuthenticationService>>();
        _optionsMock = new Mock<IOptions<ConfigReaderApiOptions>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        
        _options = new ConfigReaderApiOptions
        {
            Security = new SecurityOptions
            {
                ApiTokens = new[] { "valid-token-12345678901234567890" }
            }
        };
        
        _optionsMock.Setup(x => x.Value).Returns(_options);
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsTrue()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var validToken = "valid-token-12345678901234567890";

        // Act
        var result = service.ValidateToken(validToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var invalidToken = "invalid-token";

        // Act
        var result = service.ValidateToken(invalidToken);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateToken_EmptyOrNullToken_ReturnsFalse(string? token)
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);

        // Act
        var result = service.ValidateToken(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_ShortToken_ReturnsFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var shortToken = "short"; // 32 karakterden kısa

        // Act
        var result = service.ValidateToken(shortToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_LongToken_ReturnsFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var longToken = new string('a', 513); // 512 karakterden uzun

        // Act
        var result = service.ValidateToken(longToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_MultipleTokensInConfig_ValidatesCorrectly()
    {
        // Arrange
        _options.Security.ApiTokens = new[]
        {
            "first-valid-token-12345678901234567890",
            "second-valid-token-12345678901234567890"
        };
        
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);

        // Act & Assert
        service.ValidateToken("first-valid-token-12345678901234567890").Should().BeTrue();
        service.ValidateToken("second-valid-token-12345678901234567890").Should().BeTrue();
        service.ValidateToken("third-invalid-token-12345678901234567890").Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_NoTokensInConfig_ReturnsFalse()
    {
        // Arrange
        _options.Security.ApiTokens = Array.Empty<string>();
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var anyToken = "any-token-12345678901234567890";

        // Act
        var result = service.ValidateToken(anyToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_GeneratedToken_ShouldReturnTrue()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var generatedToken = "tk_generated123456789012345678901234567890_1234567890";
        
        // Cache'e bir generated token ekle
        _memoryCache.Set($"generated_token_{generatedToken}", new 
        { 
            Token = generatedToken, 
            IsActive = true, 
            ExpiresAt = DateTime.UtcNow.AddMinutes(30) 
        });

        // Act
        var result = service.ValidateToken(generatedToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_ExpiredGeneratedToken_ShouldReturnFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var expiredToken = "tk_expired123456789012345678901234567890_1234567890";
        
        // Cache'e expired token ekle
        _memoryCache.Set($"generated_token_{expiredToken}", new 
        { 
            Token = expiredToken, 
            IsActive = true, 
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1) // 1 dakika önce expired
        });

        // Act
        var result = service.ValidateToken(expiredToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new TokenAuthenticationService(null!, _optionsMock.Object, _memoryCache);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new TokenAuthenticationService(_loggerMock.Object, null!, _memoryCache);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullMemoryCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithValidArguments_InitializesCorrectly()
    {
        // Act & Assert
        var action = () => new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("token123456789012345678901234567890")]
    [InlineData("TOKEN123456789012345678901234567890")]
    [InlineData("Token-With-Dashes-123456789012345678901234567890")]
    [InlineData("token_with_underscores_123456789012345678901234567890")]
    public void ValidateToken_ValidTokenFormats_ReturnsTrue(string validToken)
    {
        // Arrange
        _options.Security.ApiTokens = new[] { validToken };
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);

        // Act
        var result = service.ValidateToken(validToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_TokenWithInvalidFormat_ReturnsFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var invalidToken = "token with spaces";

        // Act
        var result = service.ValidateToken(invalidToken);

        // Assert
        result.Should().BeFalse();
    }

    #region IsAuthenticationRequired Tests

    [Fact]
    public void IsAuthenticationRequired_WhenRequireAuthTrue_ReturnsTrue()
    {
        // Arrange
        _options.Security.RequireAuth = true;
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);

        // Act
        var result = service.IsAuthenticationRequired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticationRequired_WhenRequireAuthFalse_ReturnsFalse()
    {
        // Arrange
        _options.Security.RequireAuth = false;
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);

        // Act
        var result = service.IsAuthenticationRequired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_WhenAuthenticationNotRequired_AlwaysReturnsTrue()
    {
        // Arrange
        _options.Security.RequireAuth = false;
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);

        // Act & Assert
        service.ValidateToken("any-token").Should().BeTrue();
        service.ValidateToken("invalid-token").Should().BeTrue();
        service.ValidateToken("").Should().BeTrue();
        service.ValidateToken(null).Should().BeTrue();
    }

    #endregion

    #region HashToken Tests

    [Fact]
    public void HashToken_WithValidToken_ReturnsHashedValue()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var token = "test-token-12345678901234567890";

        // Act
        var hashedToken = service.HashToken(token);

        // Assert
        hashedToken.Should().NotBeNullOrEmpty();
        hashedToken.Should().NotBe(token);
        hashedToken.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void HashToken_WithSameToken_ReturnsConsistentHash()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var token = "test-token-12345678901234567890";

        // Act
        var hash1 = service.HashToken(token);
        var hash2 = service.HashToken(token);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_WithDifferentTokens_ReturnsDifferentHashes()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var token1 = "test-token-1-12345678901234567890";
        var token2 = "test-token-2-12345678901234567890";

        // Act
        var hash1 = service.HashToken(token1);
        var hash2 = service.HashToken(token2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void HashToken_WithInvalidToken_ThrowsArgumentException(string? token)
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);

        // Act & Assert
        var action = () => service.HashToken(token!);
        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region IsValidTokenFormat Tests

    [Fact]
    public void IsValidTokenFormat_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var validToken = "valid-token-12345678901234567890";

        // Act
        var result = service.IsValidTokenFormat(validToken);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void IsValidTokenFormat_WithInvalidToken_ReturnsFalse(string? token)
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);

        // Act
        var result = service.IsValidTokenFormat(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidTokenFormat_WithTooShortToken_ReturnsFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var shortToken = "short"; // 32 karakterden kısa

        // Act
        var result = service.IsValidTokenFormat(shortToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidTokenFormat_WithTooLongToken_ReturnsFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var longToken = new string('a', 513); // 512 karakterden uzun

        // Act
        var result = service.IsValidTokenFormat(longToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidTokenFormat_WithInvalidCharacters_ReturnsFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var invalidToken = "token with spaces and@invalid#characters!";

        // Act
        var result = service.IsValidTokenFormat(invalidToken);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("valid-token-12345678901234567890")]
    [InlineData("VALID_TOKEN_12345678901234567890")]
    [InlineData("valid123token456789012345678901234567890")]
    [InlineData("Valid-Token_123456789012345678901234567890")]
    public void IsValidTokenFormat_WithValidFormats_ReturnsTrue(string validToken)
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);

        // Act
        var result = service.IsValidTokenFormat(validToken);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Generated Token Edge Cases

    [Fact]
    public void ValidateToken_GeneratedTokenWithInactiveFlag_ShouldReturnFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var inactiveToken = "tk_inactive123456789012345678901234567890_1234567890";
        
        // Cache'e inactive token ekle
        _memoryCache.Set($"generated_token_{inactiveToken}", new 
        { 
            Token = inactiveToken, 
            IsActive = false, // Inactive
            ExpiresAt = DateTime.UtcNow.AddMinutes(30) 
        });

        // Act
        var result = service.ValidateToken(inactiveToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_GeneratedTokenWithNullCacheData_ShouldReturnFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var nullDataToken = "tk_nulldata123456789012345678901234567890_1234567890";
        
        // Cache'e null data ekle
        _memoryCache.Set($"generated_token_{nullDataToken}", null as object);

        // Act
        var result = service.ValidateToken(nullDataToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_GeneratedTokenWithInvalidCacheData_ShouldReturnFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        var invalidDataToken = "tk_invaliddata123456789012345678901234567890_1234567890";
        
        // Cache'e invalid data ekle
        _memoryCache.Set($"generated_token_{invalidDataToken}", new 
        { 
            Token = invalidDataToken, 
            // IsActive ve ExpiresAt eksik
        });

        // Act
        var result = service.ValidateToken(invalidDataToken);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Configuration Edge Cases

    [Fact]
    public void Constructor_WithNullSecurityOptions_InitializesWithEmptyTokens()
    {
        // Arrange
        var options = new ConfigReaderApiOptions
        {
            Security = null!
        };
        _optionsMock.Setup(x => x.Value).Returns(options);

        // Act & Assert
        var action = () => new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullApiTokens_InitializesWithEmptyTokens()
    {
        // Arrange
        var options = new ConfigReaderApiOptions
        {
            Security = new SecurityOptions { ApiTokens = null! }
        };
        _optionsMock.Setup(x => x.Value).Returns(options);

        // Act & Assert
        var action = () => new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateToken_WithEmptyTokensInConfig_FiltersOutEmptyTokens()
    {
        // Arrange
        _options.Security.ApiTokens = new[] { 
            "valid-token-12345678901234567890",
            "",
            "   ",
            null!,
            "another-valid-token-12345678901234567890"
        };
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object, _memoryCache);

        // Act & Assert
        service.ValidateToken("valid-token-12345678901234567890").Should().BeTrue();
        service.ValidateToken("another-valid-token-12345678901234567890").Should().BeTrue();
        service.ValidateToken("").Should().BeFalse();
        service.ValidateToken("   ").Should().BeFalse();
    }

    #endregion
} 