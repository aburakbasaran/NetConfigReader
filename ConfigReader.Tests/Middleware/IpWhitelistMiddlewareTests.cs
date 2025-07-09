using System.Net;
using System.Text.Json;
using ConfigReader.Api.Middleware;
using ConfigReader.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConfigReader.Tests.Middleware;

public sealed class IpWhitelistMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<IIpWhitelistService> _ipWhitelistServiceMock;
    private readonly Mock<ILogger<IpWhitelistMiddleware>> _loggerMock;
    private readonly IpWhitelistMiddleware _middleware;

    public IpWhitelistMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _ipWhitelistServiceMock = new Mock<IIpWhitelistService>();
        _loggerMock = new Mock<ILogger<IpWhitelistMiddleware>>();
        _middleware = new IpWhitelistMiddleware(_nextMock.Object, _ipWhitelistServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new IpWhitelistMiddleware(null!, _ipWhitelistServiceMock.Object, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("next");
    }

    [Fact]
    public void Constructor_WithNullIpWhitelistService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new IpWhitelistMiddleware(_nextMock.Object, null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("ipWhitelistService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new IpWhitelistMiddleware(_nextMock.Object, _ipWhitelistServiceMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task InvokeAsync_WhenWhitelistDisabled_ShouldCallNext()
    {
        // Arrange
        var context = CreateHttpContext();
        _ipWhitelistServiceMock.Setup(s => s.IsWhitelistEnabled()).Returns(false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithAllowedIP_ShouldCallNext()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");
        
        _ipWhitelistServiceMock.Setup(s => s.IsWhitelistEnabled()).Returns(true);
        _ipWhitelistServiceMock.Setup(s => s.IsIpAllowed("192.168.1.100")).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithNotAllowedIP_ShouldReturnForbidden()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("8.8.8.8");
        
        _ipWhitelistServiceMock.Setup(s => s.IsWhitelistEnabled()).Returns(true);
        _ipWhitelistServiceMock.Setup(s => s.IsIpAllowed("8.8.8.8")).Returns(false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(403);
        context.Response.ContentType.Should().Be("application/json");
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithForwardedForHeader_ShouldUseForwardedIP()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.100, 10.0.0.1";
        context.Connection.RemoteIpAddress = IPAddress.Parse("8.8.8.8");
        
        _ipWhitelistServiceMock.Setup(s => s.IsWhitelistEnabled()).Returns(true);
        _ipWhitelistServiceMock.Setup(s => s.IsIpAllowed("192.168.1.100")).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _ipWhitelistServiceMock.Verify(s => s.IsIpAllowed("192.168.1.100"), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithRealIPHeader_ShouldUseRealIP()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Real-IP"] = "10.0.0.1";
        context.Connection.RemoteIpAddress = IPAddress.Parse("8.8.8.8");
        
        _ipWhitelistServiceMock.Setup(s => s.IsWhitelistEnabled()).Returns(true);
        _ipWhitelistServiceMock.Setup(s => s.IsIpAllowed("10.0.0.1")).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _ipWhitelistServiceMock.Verify(s => s.IsIpAllowed("10.0.0.1"), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithIPv6MappedToIPv4_ShouldMapToIPv4()
    {
        // Arrange
        var context = CreateHttpContext();
        var ipv6Mapped = IPAddress.Parse("::ffff:192.168.1.100");
        context.Connection.RemoteIpAddress = ipv6Mapped;
        
        _ipWhitelistServiceMock.Setup(s => s.IsWhitelistEnabled()).Returns(true);
        _ipWhitelistServiceMock.Setup(s => s.IsIpAllowed("192.168.1.100")).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _ipWhitelistServiceMock.Verify(s => s.IsIpAllowed("192.168.1.100"), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithNullRemoteIP_ShouldReturnForbidden()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Connection.RemoteIpAddress = null;
        
        _ipWhitelistServiceMock.Setup(s => s.IsWhitelistEnabled()).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(403);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidForwardedFor_ShouldUseRemoteIP()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "invalid-ip";
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");
        
        _ipWhitelistServiceMock.Setup(s => s.IsWhitelistEnabled()).Returns(true);
        _ipWhitelistServiceMock.Setup(s => s.IsIpAllowed("192.168.1.100")).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _ipWhitelistServiceMock.Verify(s => s.IsIpAllowed("192.168.1.100"), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleForwardedIPs_ShouldUseFirst()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.100, 10.0.0.1, 172.16.0.1";
        
        _ipWhitelistServiceMock.Setup(s => s.IsWhitelistEnabled()).Returns(true);
        _ipWhitelistServiceMock.Setup(s => s.IsIpAllowed("192.168.1.100")).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _ipWhitelistServiceMock.Verify(s => s.IsIpAllowed("192.168.1.100"), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Theory]
    [InlineData("::1")]
    [InlineData("2001:db8::1")]
    public async Task InvokeAsync_WithIPv6Address_ShouldWork(string ipv6Address)
    {
        // Arrange
        var context = CreateHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ipv6Address);
        
        _ipWhitelistServiceMock.Setup(s => s.IsWhitelistEnabled()).Returns(true);
        _ipWhitelistServiceMock.Setup(s => s.IsIpAllowed(ipv6Address)).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _ipWhitelistServiceMock.Verify(s => s.IsIpAllowed(ipv6Address), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithNotAllowedIP_ShouldReturnCorrectErrorMessage()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("8.8.8.8");
        
        _ipWhitelistServiceMock.Setup(s => s.IsWhitelistEnabled()).Returns(true);
        _ipWhitelistServiceMock.Setup(s => s.IsIpAllowed("8.8.8.8")).Returns(false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(403);
        
        var responseBody = await ReadResponseBodyAsync(context);
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        errorResponse.GetProperty("error").GetString().Should().Be("Access Denied");
        errorResponse.GetProperty("message").GetString().Should().Be("Access denied: IP address not in whitelist");
        errorResponse.GetProperty("statusCode").GetInt32().Should().Be(403);
        errorResponse.GetProperty("timestamp").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyForwardedFor_ShouldUseRemoteIP()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "";
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");
        
        _ipWhitelistServiceMock.Setup(s => s.IsWhitelistEnabled()).Returns(true);
        _ipWhitelistServiceMock.Setup(s => s.IsIpAllowed("192.168.1.100")).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _ipWhitelistServiceMock.Verify(s => s.IsIpAllowed("192.168.1.100"), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
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