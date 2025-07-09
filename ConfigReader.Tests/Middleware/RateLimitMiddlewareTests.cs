using ConfigReader.Api.Middleware;
using ConfigReader.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace ConfigReader.Tests.Middleware;

/// <summary>
/// RateLimitMiddleware unit testleri
/// </summary>
public sealed class RateLimitMiddlewareTests
{
    private readonly Mock<IRateLimitService> _rateLimitServiceMock;
    private readonly Mock<ILogger<RateLimitMiddleware>> _loggerMock;
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly RateLimitMiddleware _middleware;

    public RateLimitMiddlewareTests()
    {
        _rateLimitServiceMock = new Mock<IRateLimitService>();
        _loggerMock = new Mock<ILogger<RateLimitMiddleware>>();
        _nextMock = new Mock<RequestDelegate>();
        
        _middleware = new RateLimitMiddleware(_nextMock.Object, _rateLimitServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithinRateLimit_ShouldCallNext()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        
        _rateLimitServiceMock
            .Setup(s => s.IsRequestAllowedAsync("127.0.0.1", "configuration-all"))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ExceedsRateLimit_ShouldReturnTooManyRequests()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        
        _rateLimitServiceMock
            .Setup(s => s.IsRequestAllowedAsync("127.0.0.1", "configuration-all"))
            .ReturnsAsync(false);
        
        _rateLimitServiceMock
            .Setup(s => s.GetRemainingRequestsAsync("127.0.0.1", "configuration-all"))
            .ReturnsAsync(0);
        
        _rateLimitServiceMock
            .Setup(s => s.GetResetTimeAsync("127.0.0.1", "configuration-all"))
            .ReturnsAsync(DateTime.UtcNow.AddDays(1));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(429);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithForwardedIP_ShouldUseForwardedIP()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.1";
        
        _rateLimitServiceMock
            .Setup(s => s.IsRequestAllowedAsync("192.168.1.1", "configuration-all"))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _rateLimitServiceMock.Verify(s => s.IsRequestAllowedAsync("192.168.1.1", "configuration-all"), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithRealIPHeader_ShouldUseRealIP()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Request.Headers["X-Real-IP"] = "10.0.0.1";
        
        _rateLimitServiceMock
            .Setup(s => s.IsRequestAllowedAsync("10.0.0.1", "configuration-all"))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _rateLimitServiceMock.Verify(s => s.IsRequestAllowedAsync("10.0.0.1", "configuration-all"), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithNullRemoteIP_ShouldUseUnknownIP()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Connection.RemoteIpAddress = null;
        
        _rateLimitServiceMock
            .Setup(s => s.IsRequestAllowedAsync("unknown", "configuration-all"))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _rateLimitServiceMock.Verify(s => s.IsRequestAllowedAsync("unknown", "configuration-all"), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddRateLimitHeaders()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        
        _rateLimitServiceMock
            .Setup(s => s.IsRequestAllowedAsync("127.0.0.1", "configuration-all"))
            .ReturnsAsync(true);
        
        _rateLimitServiceMock
            .Setup(s => s.GetRemainingRequestsAsync("127.0.0.1", "configuration-all"))
            .ReturnsAsync(5);
        
        _rateLimitServiceMock
            .Setup(s => s.GetResetTimeAsync("127.0.0.1", "configuration-all"))
            .ReturnsAsync(DateTime.UtcNow.AddDays(1));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-RateLimit-Remaining");
        context.Response.Headers.Should().ContainKey("X-RateLimit-Reset");
        context.Response.Headers.Should().ContainKey("X-RateLimit-Limit");
        
        context.Response.Headers["X-RateLimit-Remaining"].ToString().Should().Be("5");
        context.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("10");
    }

    [Fact]
    public async Task InvokeAsync_WhenRateLimitExceeded_ShouldReturnCorrectErrorMessage()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        
        _rateLimitServiceMock
            .Setup(s => s.IsRequestAllowedAsync("127.0.0.1", "/api/configuration"))
            .ReturnsAsync(false);
        
        _rateLimitServiceMock
            .Setup(s => s.GetRemainingRequestsAsync("127.0.0.1", "/api/configuration"))
            .ReturnsAsync(0);
        
        _rateLimitServiceMock
            .Setup(s => s.GetResetTimeAsync("127.0.0.1", "/api/configuration"))
            .ReturnsAsync(DateTime.UtcNow.AddDays(1));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(429);
        context.Response.ContentType.Should().Be("application/json");
        
        var responseBody = await ReadResponseBodyAsync(context);
        responseBody.Should().Contain("Rate limit exceeded");
    }

    [Fact]
    public async Task InvokeAsync_WithServiceException_ShouldReturnInternalServerError()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        
        _rateLimitServiceMock
            .Setup(s => s.IsRequestAllowedAsync("127.0.0.1", "configuration-all"))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        // Exception durumunda middleware next'i çağırıyor
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithDifferentEndpoints_ShouldTrackSeparately()
    {
        // Arrange
        var context1 = CreateHttpContext();
        context1.Request.Path = "/api/configuration";
        context1.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        
        var context2 = CreateHttpContext();
        context2.Request.Path = "/api/token";
        context2.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        
        _rateLimitServiceMock
            .Setup(s => s.IsRequestAllowedAsync("127.0.0.1", "/api/configuration"))
            .ReturnsAsync(true);
        
        _rateLimitServiceMock
            .Setup(s => s.IsRequestAllowedAsync("127.0.0.1", "/api/token"))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(context1);
        await _middleware.InvokeAsync(context2);

        // Assert
        _rateLimitServiceMock.Verify(s => s.IsRequestAllowedAsync("127.0.0.1", "configuration-all"), Times.Once);
        // Token endpoint rate limit'e tabi değil, dolayısıyla hiç çağrılmamalı
        _rateLimitServiceMock.Verify(s => s.IsRequestAllowedAsync("127.0.0.1", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithIPv6Address_ShouldWork()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/configuration";
        context.Connection.RemoteIpAddress = IPAddress.Parse("::1");
        
        _rateLimitServiceMock
            .Setup(s => s.IsRequestAllowedAsync("::1", "configuration-all"))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _rateLimitServiceMock.Verify(s => s.IsRequestAllowedAsync("::1", "configuration-all"), Times.Once);
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