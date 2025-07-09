using ConfigReader.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ConfigReader.Tests.Services;

/// <summary>
/// RateLimitService unit testleri
/// </summary>
public sealed class RateLimitServiceTests
{
    private readonly Mock<ILogger<RateLimitService>> _loggerMock;
    private readonly RateLimitService _rateLimitService;

    public RateLimitServiceTests()
    {
        _loggerMock = new Mock<ILogger<RateLimitService>>();
        _rateLimitService = new RateLimitService(_loggerMock.Object);
    }

    [Fact]
    public async Task IsRequestAllowedAsync_FirstRequest_ReturnsTrue()
    {
        // Arrange
        var clientId = "test-client";
        var endpoint = "test-endpoint";

        // Act
        var result = await _rateLimitService.IsRequestAllowedAsync(clientId, endpoint);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsRequestAllowedAsync_WithinLimit_ReturnsTrue()
    {
        // Arrange
        var clientId = "test-client";
        var endpoint = "test-endpoint";

        // Act - 10 request (limit dahilinde)
        for (int i = 0; i < 10; i++)
        {
            var result = await _rateLimitService.IsRequestAllowedAsync(clientId, endpoint);
            result.Should().BeTrue();
        }
    }

    [Fact]
    public async Task IsRequestAllowedAsync_ExceedsLimit_ReturnsFalse()
    {
        // Arrange
        var clientId = "test-client";
        var endpoint = "test-endpoint";

        // Act - 11 request (limit aşıldı)
        for (int i = 0; i < 10; i++)
        {
            await _rateLimitService.IsRequestAllowedAsync(clientId, endpoint);
        }

        var result = await _rateLimitService.IsRequestAllowedAsync(clientId, endpoint);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRequestAllowedAsync_DifferentEndpoints_SeparateCounters()
    {
        // Arrange
        var clientId = "test-client";
        var endpoint1 = "endpoint1";
        var endpoint2 = "endpoint2";

        // Act - endpoint1 için 10 request
        for (int i = 0; i < 10; i++)
        {
            await _rateLimitService.IsRequestAllowedAsync(clientId, endpoint1);
        }

        // endpoint2 için ilk request
        var result = await _rateLimitService.IsRequestAllowedAsync(clientId, endpoint2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsRequestAllowedAsync_DifferentClients_SeparateCounters()
    {
        // Arrange
        var client1 = "client1";
        var client2 = "client2";
        var endpoint = "test-endpoint";

        // Act - client1 için 10 request
        for (int i = 0; i < 10; i++)
        {
            await _rateLimitService.IsRequestAllowedAsync(client1, endpoint);
        }

        // client2 için ilk request
        var result = await _rateLimitService.IsRequestAllowedAsync(client2, endpoint);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetRemainingRequestsAsync_NewClient_ReturnsFullLimit()
    {
        // Arrange
        var clientId = "test-client";
        var endpoint = "test-endpoint";

        // Act
        var remaining = await _rateLimitService.GetRemainingRequestsAsync(clientId, endpoint);

        // Assert
        remaining.Should().Be(10);
    }

    [Fact]
    public async Task GetRemainingRequestsAsync_AfterRequests_ReturnsCorrectCount()
    {
        // Arrange
        var clientId = "test-client";
        var endpoint = "test-endpoint";

        // Act - 3 request yap
        for (int i = 0; i < 3; i++)
        {
            await _rateLimitService.IsRequestAllowedAsync(clientId, endpoint);
        }

        var remaining = await _rateLimitService.GetRemainingRequestsAsync(clientId, endpoint);

        // Assert
        remaining.Should().Be(7);
    }

    [Fact]
    public async Task GetRemainingRequestsAsync_ExceedsLimit_ReturnsZero()
    {
        // Arrange
        var clientId = "test-client";
        var endpoint = "test-endpoint";

        // Act - 15 request yap (limit aşıldı)
        for (int i = 0; i < 15; i++)
        {
            await _rateLimitService.IsRequestAllowedAsync(clientId, endpoint);
        }

        var remaining = await _rateLimitService.GetRemainingRequestsAsync(clientId, endpoint);

        // Assert
        remaining.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task IsRequestAllowedAsync_EmptyClientId_ReturnsTrue(string clientId)
    {
        // Arrange
        var endpoint = "test-endpoint";

        // Act
        var result = await _rateLimitService.IsRequestAllowedAsync(clientId, endpoint);

        // Assert - Empty client ID işleme devam eder ve true döner
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task IsRequestAllowedAsync_EmptyEndpoint_ReturnsTrue(string endpoint)
    {
        // Arrange
        var clientId = "test-client";

        // Act
        var result = await _rateLimitService.IsRequestAllowedAsync(clientId, endpoint);

        // Assert - Empty endpoint işleme devam eder ve true döner
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentRequests_ThreadSafe()
    {
        // Arrange
        var clientId = "test-client";
        var endpoint = "test-endpoint";
        var tasks = new List<Task<bool>>();

        // Act - 20 concurrent request
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(_rateLimitService.IsRequestAllowedAsync(clientId, endpoint));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - sadece 10 tanesi true olmalı
        results.Count(r => r).Should().Be(10);
        results.Count(r => !r).Should().Be(10);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new RateLimitService(null!);
        action.Should().Throw<ArgumentNullException>();
    }
} 