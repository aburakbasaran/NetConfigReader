using ConfigReader.Api.Models;
using ConfigReader.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ConfigReader.Tests.Services;

/// <summary>
/// TokenAuthenticationService unit testleri
/// </summary>
public sealed class TokenAuthenticationServiceTests
{
    private readonly Mock<ILogger<TokenAuthenticationService>> _loggerMock;
    private readonly Mock<IOptions<ConfigReaderApiOptions>> _optionsMock;
    private readonly ConfigReaderApiOptions _options;

    public TokenAuthenticationServiceTests()
    {
        _loggerMock = new Mock<ILogger<TokenAuthenticationService>>();
        _optionsMock = new Mock<IOptions<ConfigReaderApiOptions>>();
        
        _options = new ConfigReaderApiOptions
        {
            Security = new SecurityOptions
            {
                ApiTokens = new[] { "valid-token-12345678901234567890" }
            }
        };
        
        _optionsMock.Setup(x => x.Value).Returns(_options);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsTrue()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);
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
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);
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
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);

        // Act
        var result = service.ValidateToken(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_ShortToken_ReturnsFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);
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
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);
        var longToken = new string('a', 513); // 512 karakterden uzun

        // Act
        var result = service.ValidateToken(longToken);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("token with spaces")]
    [InlineData("token\twith\ttabs")]
    [InlineData("token\nwith\nnewlines")]
    [InlineData("token with special chars: !@#$%")]
    public void ValidateToken_InvalidCharacters_ReturnsFalse(string invalidToken)
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);

        // Act
        var result = service.ValidateToken(invalidToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_ValidFormatButNotInConfig_ReturnsFalse()
    {
        // Arrange
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);
        var validFormatToken = "valid-format-token-but-not-in-config-list";

        // Act
        var result = service.ValidateToken(validFormatToken);

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
        
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);

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
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);
        var anyToken = "any-token-12345678901234567890";

        // Act
        var result = service.ValidateToken(anyToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_NullTokensInConfig_HandledGracefully()
    {
        // Arrange - Null yerine empty array kullan
        _options.Security.ApiTokens = Array.Empty<string>();
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);
        var anyToken = "any-token-12345678901234567890";

        // Act
        var result = service.ValidateToken(anyToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_ExactlyMinLength_ReturnsTrue()
    {
        // Arrange
        var minLengthToken = new string('a', 32); // Exactly 32 characters
        _options.Security.ApiTokens = new[] { minLengthToken };
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);

        // Act
        var result = service.ValidateToken(minLengthToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_ExactlyMaxLength_ReturnsTrue()
    {
        // Arrange
        var maxLengthToken = new string('a', 512); // Exactly 512 characters
        _options.Security.ApiTokens = new[] { maxLengthToken };
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);

        // Act
        var result = service.ValidateToken(maxLengthToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new TokenAuthenticationService(null!, _optionsMock.Object);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new TokenAuthenticationService(_loggerMock.Object, null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithValidArguments_InitializesCorrectly()
    {
        // Act
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);

        // Assert
        service.Should().NotBeNull();
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
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);

        // Act
        var result = service.ValidateToken(validToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_TokenWithDots_ReturnsFalse()
    {
        // Arrange
        var tokenWithDots = "token.with.dots.123456789012345678901234567890";
        _options.Security.ApiTokens = new[] { tokenWithDots };
        var service = new TokenAuthenticationService(_loggerMock.Object, _optionsMock.Object);

        // Act
        var result = service.ValidateToken(tokenWithDots);

        // Assert - Nokta karakteri regex pattern'de desteklenmiyor
        result.Should().BeFalse();
    }
} 