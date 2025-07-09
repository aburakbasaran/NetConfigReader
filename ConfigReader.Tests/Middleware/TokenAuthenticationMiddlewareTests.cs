using ConfigReader.Api.Middleware;
using ConfigReader.Api.Models;
using ConfigReader.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Xunit;

namespace ConfigReader.Tests.Middleware;

/// <summary>
/// TokenAuthenticationMiddleware unit testleri
/// </summary>
public sealed class TokenAuthenticationMiddlewareTests
{
    private readonly Mock<ITokenAuthenticationService> _tokenAuthServiceMock;
    private readonly Mock<ILogger<TokenAuthenticationMiddleware>> _loggerMock;
    private readonly Mock<IOptions<ConfigReaderApiOptions>> _optionsMock;
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly TokenAuthenticationMiddleware _middleware;
    private readonly ConfigReaderApiOptions _options;

    public TokenAuthenticationMiddlewareTests()
    {
        _tokenAuthServiceMock = new Mock<ITokenAuthenticationService>();
        _loggerMock = new Mock<ILogger<TokenAuthenticationMiddleware>>();
        _optionsMock = new Mock<IOptions<ConfigReaderApiOptions>>();
        _nextMock = new Mock<RequestDelegate>();
        
        _options = new ConfigReaderApiOptions
        {
            Security = new SecurityOptions
            {
                TokenHeaderName = "Authorization",
                RequireAuth = true
            }
        };
        
        _optionsMock.Setup(x => x.Value).Returns(_options);
        
        _middleware = new TokenAuthenticationMiddleware(_nextMock.Object, _loggerMock.Object, _tokenAuthServiceMock.Object, _optionsMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithValidToken_ShouldCallNext()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Request.Headers["Authorization"] = "Bearer valid-token";
        
        _tokenAuthServiceMock.Setup(s => s.IsAuthenticationRequired()).Returns(true);
        _tokenAuthServiceMock.Setup(s => s.ValidateToken("Bearer valid-token")).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Request.Headers["Authorization"] = "Bearer invalid-token";
        
        _tokenAuthServiceMock.Setup(s => s.IsAuthenticationRequired()).Returns(true);
        _tokenAuthServiceMock.Setup(s => s.ValidateToken("Bearer invalid-token")).Returns(false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        
        _tokenAuthServiceMock.Setup(s => s.IsAuthenticationRequired()).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_AuthNotRequired_ShouldCallNext()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        
        _tokenAuthServiceMock.Setup(s => s.IsAuthenticationRequired()).Returns(false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithPublicEndpoint_ShouldCallNext()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/token";
        
        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithCustomTokenHeader_ShouldValidateToken()
    {
        // Arrange
        _options.Security.TokenHeaderName = "X-API-Key";
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Request.Headers["X-API-Key"] = "api-key-token";
        
        _tokenAuthServiceMock.Setup(s => s.IsAuthenticationRequired()).Returns(true);
        _tokenAuthServiceMock.Setup(s => s.ValidateToken("api-key-token")).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _tokenAuthServiceMock.Verify(s => s.ValidateToken("api-key-token"), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithNonAuthenticatedEndpoint_ShouldCallNext()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/health";
        
        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Request.Headers["Authorization"] = "";
        
        _tokenAuthServiceMock.Setup(s => s.IsAuthenticationRequired()).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithWhitespaceToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Request.Headers["Authorization"] = "   ";
        
        _tokenAuthServiceMock.Setup(s => s.IsAuthenticationRequired()).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithGeneratedToken_ShouldValidateSuccessfully()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        var generatedToken = "tk_generated123456789012345678901234567890_1234567890";
        context.Request.Headers["Authorization"] = $"Bearer {generatedToken}";
        
        _tokenAuthServiceMock.Setup(s => s.IsAuthenticationRequired()).Returns(true);
        _tokenAuthServiceMock.Setup(s => s.ValidateToken($"Bearer {generatedToken}")).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _tokenAuthServiceMock.Verify(s => s.ValidateToken($"Bearer {generatedToken}"), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldWriteCorrectUnauthorizedMessage()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        
        _tokenAuthServiceMock.Setup(s => s.IsAuthenticationRequired()).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        context.Response.ContentType.Should().Be("application/json");
        
        var responseBody = await ReadResponseBodyAsync(context);
        responseBody.Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task InvokeAsync_WithServiceException_ShouldReturnInternalServerError()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Request.Headers["Authorization"] = "Bearer token";
        
        _tokenAuthServiceMock.Setup(s => s.IsAuthenticationRequired()).Returns(true);
        _tokenAuthServiceMock.Setup(s => s.ValidateToken("Bearer token")).Throws(new Exception("Service error"));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
} 