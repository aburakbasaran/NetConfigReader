using ConfigReader.Api.Controllers;
using ConfigReader.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ConfigReader.Tests.Controllers;

/// <summary>
/// TokenController testleri
/// </summary>
public class TokenControllerTests
{
    private readonly Mock<ITokenGeneratorService> _tokenGeneratorServiceMock;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly ILogger<TokenController> _logger;
    private readonly TokenController _controller;

    public TokenControllerTests()
    {
        _tokenGeneratorServiceMock = new Mock<ITokenGeneratorService>();
        _environmentMock = new Mock<IWebHostEnvironment>();
        _logger = new NullLogger<TokenController>();
        
        // Default configuration - Development environment
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        
        _controller = new TokenController(_tokenGeneratorServiceMock.Object, _environmentMock.Object, _logger);
    }

    #region Generate Token Tests

    [Fact]
    public async Task GenerateToken_InDevelopment_ShouldReturnTokenResponse()
    {
        // Arrange
        const string expectedToken = "tk_test_token_123";
        const int expiryMinutes = 1;
        
        _tokenGeneratorServiceMock
            .Setup(s => s.GenerateTokenAsync(expiryMinutes))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _controller.GenerateToken(expiryMinutes);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateToken_InProduction_ShouldReturnNotFound()
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

        // Act
        var result = await _controller.GenerateToken();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1441)] // Above 24 hours
    public async Task GenerateToken_WithInvalidExpiryTimes_ShouldReturnBadRequest(int expiryMinutes)
    {
        // Act
        var result = await _controller.GenerateToken(expiryMinutes);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region List Tokens Tests

    [Fact]
    public async Task ListActiveTokens_InDevelopment_ShouldReturnActiveTokens()
    {
        // Arrange
        var expectedTokens = new List<string> { "tk_token1", "tk_token2" };
        
        _tokenGeneratorServiceMock
            .Setup(s => s.GetActiveTokensAsync())
            .ReturnsAsync(expectedTokens);

        // Act
        var result = await _controller.ListActiveTokens();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task ListActiveTokens_InProduction_ShouldReturnNotFound()
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

        // Act
        var result = await _controller.ListActiveTokens();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Revoke Token Tests

    [Fact]
    public async Task RevokeToken_InDevelopment_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        const string token = "tk_valid_token";

        // Act
        var result = await _controller.RevokeToken(token);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        _tokenGeneratorServiceMock.Verify(s => s.RevokeTokenAsync(token), Times.Once);
    }

    [Fact]
    public async Task RevokeToken_InProduction_ShouldReturnNotFound()
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

        // Act
        var result = await _controller.RevokeToken("tk_any_token");

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RevokeToken_WithInvalidToken_ShouldReturnBadRequest(string token)
    {
        // Act
        var result = await _controller.RevokeToken(token);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Environment Tests

    [Fact]
    public async Task TokenEndpoints_InDevelopment_ShouldWork()
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        
        const string expectedToken = "tk_test_token";
        var expectedTokens = new List<string> { expectedToken };
        
        _tokenGeneratorServiceMock
            .Setup(s => s.GenerateTokenAsync(It.IsAny<int>()))
            .ReturnsAsync(expectedToken);
        
        _tokenGeneratorServiceMock
            .Setup(s => s.GetActiveTokensAsync())
            .ReturnsAsync(expectedTokens);

        // Act
        var generateResult = await _controller.GenerateToken();
        var listResult = await _controller.ListActiveTokens();

        // Assert
        generateResult.Should().BeOfType<OkObjectResult>();
        listResult.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TokenEndpoints_InProduction_ShouldReturnNotFound()
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

        // Act
        var generateResult = await _controller.GenerateToken();
        var listResult = await _controller.ListActiveTokens();
        var revokeResult = await _controller.RevokeToken("tk_any_token");

        // Assert
        generateResult.Should().BeOfType<NotFoundObjectResult>();
        listResult.Should().BeOfType<NotFoundObjectResult>();
        revokeResult.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Token Cleanup Tests

    [Fact]
    public async Task GenerateToken_MultipleRequests_ShouldCallCleanupEachTime()
    {
        // Arrange
        const int requestCount = 3;
        _tokenGeneratorServiceMock
            .Setup(s => s.GenerateTokenAsync(It.IsAny<int>()))
            .ReturnsAsync("tk_new_token");

        // Act
        for (int i = 0; i < requestCount; i++)
        {
            await _controller.GenerateToken();
        }

        // Assert
        _tokenGeneratorServiceMock.Verify(s => s.GenerateTokenAsync(It.IsAny<int>()), Times.Exactly(requestCount));
    }

    [Fact]
    public async Task GenerateToken_ShouldUseOneMinuteDefaultExpiry()
    {
        // Arrange
        const int defaultExpiry = 1;
        const string expectedToken = "tk_default_token";
        
        _tokenGeneratorServiceMock
            .Setup(s => s.GenerateTokenAsync(defaultExpiry))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _controller.GenerateToken(); // No expiry parameter

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _tokenGeneratorServiceMock.Verify(s => s.GenerateTokenAsync(defaultExpiry), Times.Once);
    }

    #endregion
} 