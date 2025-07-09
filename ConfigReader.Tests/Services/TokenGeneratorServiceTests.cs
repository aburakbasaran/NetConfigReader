using ConfigReader.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConfigReader.Tests.Services;

/// <summary>
/// TokenGeneratorService testleri
/// </summary>
public class TokenGeneratorServiceTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<TokenGeneratorService> _logger;
    private readonly TokenGeneratorService _service;

    public TokenGeneratorServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _logger = new NullLogger<TokenGeneratorService>();
        _service = new TokenGeneratorService(_memoryCache, _logger);
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Token Generation Tests

    [Fact]
    public async Task GenerateTokenAsync_ShouldReturnValidToken()
    {
        // Act
        var token = await _service.GenerateTokenAsync();

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().StartWith("tk_");
        token.Length.Should().BeGreaterThan(20);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithCustomExpiry_ShouldRespectExpiryTime()
    {
        // Arrange
        const int expiryMinutes = 5;

        // Act
        var token = await _service.GenerateTokenAsync(expiryMinutes);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        // Token geçerli olmalı
        var isValid = await _service.IsTokenValidAsync(token);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateTokenAsync_ShouldGenerateUniqueTokens()
    {
        // Act
        var token1 = await _service.GenerateTokenAsync();
        var token2 = await _service.GenerateTokenAsync();

        // Assert
        token1.Should().NotBe(token2);
        token1.Should().StartWith("tk_");
        token2.Should().StartWith("tk_");
    }

    [Fact]
    public async Task GenerateTokenAsync_ShouldCleanupPreviousTokens()
    {
        // Arrange
        var firstToken = await _service.GenerateTokenAsync(5);
        
        // Verify first token is valid
        var isFirstValid = await _service.IsTokenValidAsync(firstToken);
        isFirstValid.Should().BeTrue();

        // Act - Generate second token (should cleanup first)
        var secondToken = await _service.GenerateTokenAsync(5);

        // Assert
        secondToken.Should().NotBe(firstToken);
        
        // First token should be cleaned up
        var isFirstStillValid = await _service.IsTokenValidAsync(firstToken);
        isFirstStillValid.Should().BeFalse();
        
        // Second token should be valid
        var isSecondValid = await _service.IsTokenValidAsync(secondToken);
        isSecondValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(60)]
    public async Task GenerateTokenAsync_WithDifferentExpiryTimes_ShouldWork(int expiryMinutes)
    {
        // Act
        var token = await _service.GenerateTokenAsync(expiryMinutes);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().StartWith("tk_");
        
        var isValid = await _service.IsTokenValidAsync(token);
        isValid.Should().BeTrue();
    }

    #endregion

    #region Token Validation Tests

    [Fact]
    public async Task IsTokenValidAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var token = await _service.GenerateTokenAsync(5);

        // Act
        var isValid = await _service.IsTokenValidAsync(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenValidAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Act
        var isValid = await _service.IsTokenValidAsync("tk_invalid_token");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenValidAsync_WithNullToken_ShouldReturnFalse()
    {
        // Act
        var isValid = await _service.IsTokenValidAsync(null!);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenValidAsync_WithEmptyToken_ShouldReturnFalse()
    {
        // Act
        var isValid = await _service.IsTokenValidAsync("");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenValidAsync_WithWhitespaceToken_ShouldReturnFalse()
    {
        // Act
        var isValid = await _service.IsTokenValidAsync("   ");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenValidAsync_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange - Generate token with very short expiry
        var token = await _service.GenerateTokenAsync(1); // 1 minute
        
        // Manually expire the token by manipulating cache
        var cacheKey = $"generated_token_{token}";
        _memoryCache.Remove(cacheKey);

        // Act
        var isValid = await _service.IsTokenValidAsync(token);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Token Revocation Tests

    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_ShouldInvalidateToken()
    {
        // Arrange
        var token = await _service.GenerateTokenAsync(5);
        
        // Verify token is initially valid
        var isValidBefore = await _service.IsTokenValidAsync(token);
        isValidBefore.Should().BeTrue();

        // Act
        await _service.RevokeTokenAsync(token);

        // Assert
        var isValidAfter = await _service.IsTokenValidAsync(token);
        isValidAfter.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeTokenAsync_WithInvalidToken_ShouldNotThrow()
    {
        // Act & Assert
        var act = async () => await _service.RevokeTokenAsync("tk_invalid_token");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeTokenAsync_WithNullToken_ShouldNotThrow()
    {
        // Act & Assert
        var act = async () => await _service.RevokeTokenAsync(null!);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeTokenAsync_WithEmptyToken_ShouldNotThrow()
    {
        // Act & Assert
        var act = async () => await _service.RevokeTokenAsync("");
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Active Tokens Tests

    [Fact]
    public async Task GetActiveTokensAsync_WithNoTokens_ShouldReturnEmptyList()
    {
        // Act
        var activeTokens = await _service.GetActiveTokensAsync();

        // Assert
        activeTokens.Should().NotBeNull();
        activeTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveTokensAsync_WithOneToken_ShouldReturnSingleToken()
    {
        // Arrange
        var token = await _service.GenerateTokenAsync(5);

        // Act
        var activeTokens = await _service.GetActiveTokensAsync();

        // Assert
        activeTokens.Should().NotBeNull();
        activeTokens.Should().HaveCount(1);
        activeTokens.Should().Contain(token);
    }

    [Fact]
    public async Task GetActiveTokensAsync_AfterRevoke_ShouldNotContainRevokedToken()
    {
        // Arrange
        var token = await _service.GenerateTokenAsync(5);
        
        // Verify token is in active list
        var activeTokensBefore = await _service.GetActiveTokensAsync();
        activeTokensBefore.Should().Contain(token);

        // Act
        await _service.RevokeTokenAsync(token);
        var activeTokensAfter = await _service.GetActiveTokensAsync();

        // Assert
        activeTokensAfter.Should().NotContain(token);
    }

    [Fact]
    public async Task GetActiveTokensAsync_AfterGeneratingNewToken_ShouldContainOnlyNewToken()
    {
        // Arrange
        var firstToken = await _service.GenerateTokenAsync(5);
        
        // Verify first token is in list
        var tokensAfterFirst = await _service.GetActiveTokensAsync();
        tokensAfterFirst.Should().Contain(firstToken);
        tokensAfterFirst.Should().HaveCount(1);

        // Act - Generate second token (should cleanup first)
        var secondToken = await _service.GenerateTokenAsync(5);
        var tokensAfterSecond = await _service.GetActiveTokensAsync();

        // Assert
        tokensAfterSecond.Should().NotContain(firstToken);
        tokensAfterSecond.Should().Contain(secondToken);
        tokensAfterSecond.Should().HaveCount(1);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task GenerateTokenAsync_ConcurrentCalls_ShouldGenerateUniqueTokens()
    {
        // Arrange
        const int concurrentCalls = 10;
        var tasks = new List<Task<string>>();

        // Act
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(_service.GenerateTokenAsync(5));
        }

        var tokens = await Task.WhenAll(tasks);

        // Assert
        tokens.Should().HaveCount(concurrentCalls);
        tokens.Should().OnlyHaveUniqueItems();
        
        foreach (var token in tokens)
        {
            token.Should().StartWith("tk_");
        }
        
        // Only the last generated token should be active (due to cleanup)
        var activeTokens = await _service.GetActiveTokensAsync();
        activeTokens.Should().HaveCount(1);
    }

    [Fact]
    public async Task TokenOperations_ThreadSafety_ShouldNotThrow()
    {
        // Arrange
        const int operationCount = 50;
        var tasks = new List<Task>();

        // Act - Mix of different operations
        for (int i = 0; i < operationCount; i++)
        {
            if (i % 3 == 0)
            {
                tasks.Add(Task.Run(async () => await _service.GenerateTokenAsync(2)));
            }
            else if (i % 3 == 1)
            {
                tasks.Add(Task.Run(async () => await _service.GetActiveTokensAsync()));
            }
            else
            {
                tasks.Add(Task.Run(async () => await _service.IsTokenValidAsync("tk_test")));
            }
        }

        // Assert - Should not throw any exceptions
        var act = async () => await Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(0)] // Minimum boundary
    [InlineData(-1)] // Invalid negative
    [InlineData(1441)] // Above max (24 hours)
    public async Task GenerateTokenAsync_WithBoundaryValues_ShouldHandleGracefully(int expiryMinutes)
    {
        // Act & Assert - Should not throw
        var act = async () => await _service.GenerateTokenAsync(expiryMinutes);
        await act.Should().NotThrowAsync();
        
        if (expiryMinutes > 0)
        {
            var token = await _service.GenerateTokenAsync(expiryMinutes);
            token.Should().NotBeNullOrEmpty();
        }
    }

    #endregion

    #region Token Format Tests

    [Fact]
    public async Task GenerateTokenAsync_TokenFormat_ShouldFollowPattern()
    {
        // Act
        var token = await _service.GenerateTokenAsync();

        // Assert
        token.Should().MatchRegex(@"^tk_[a-f0-9]{32}_\d+$");
    }

    [Fact]
    public async Task GenerateTokenAsync_MultipleTokens_ShouldHaveSameFormat()
    {
        // Arrange
        const int tokenCount = 5;
        var tokens = new List<string>();

        // Act
        for (int i = 0; i < tokenCount; i++)
        {
            var token = await _service.GenerateTokenAsync(5);
            tokens.Add(token);
        }

        // Assert
        foreach (var token in tokens)
        {
            token.Should().StartWith("tk_");
            token.Split('_').Should().HaveCount(3);
            token.Split('_')[1].Length.Should().Be(32); // GUID length
            var parts = token.Split('_');
            long.TryParse(parts[2], out _).Should().BeTrue(); // Timestamp should be parseable
        }
    }

    #endregion

    #region Memory Management Tests

    [Fact]
    public async Task TokenGeneration_MemoryUsage_ShouldNotAccumulateOldTokens()
    {
        // Arrange
        const int iterations = 20;
        var initialMemory = GC.GetTotalMemory(true);

        // Act - Generate many tokens (each should cleanup previous)
        for (int i = 0; i < iterations; i++)
        {
            await _service.GenerateTokenAsync(1);
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(true);

        // Assert - Should only have one active token
        var activeTokens = await _service.GetActiveTokensAsync();
        activeTokens.Should().HaveCount(1);
        
        // Memory shouldn't grow significantly (allowing for some variance)
        var memoryGrowth = finalMemory - initialMemory;
        memoryGrowth.Should().BeLessThan(1024 * 1024); // Less than 1MB growth
    }

    #endregion
} 