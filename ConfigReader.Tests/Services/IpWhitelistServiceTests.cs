using System.Net;
using ConfigReader.Api.Models;
using ConfigReader.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ConfigReader.Tests.Services;

public sealed class IpWhitelistServiceTests
{
    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<IpWhitelistService>>();

        // Act & Assert
        var act = () => new IpWhitelistService(null!, logger.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = CreateOptionsMonitor(new SecurityOptions());

        // Act & Assert
        var act = () => new IpWhitelistService(options, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsWhitelistEnabled_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var securityOptions = new SecurityOptions { EnableIpWhitelist = false };
        var service = CreateService(securityOptions);

        // Act & Assert
        service.IsWhitelistEnabled().Should().BeFalse();
    }

    [Fact]
    public void IsWhitelistEnabled_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var securityOptions = new SecurityOptions { EnableIpWhitelist = true };
        var service = CreateService(securityOptions);

        // Act & Assert
        service.IsWhitelistEnabled().Should().BeTrue();
    }

    [Fact]
    public void IsIpAllowed_WhenWhitelistDisabled_ReturnsTrue()
    {
        // Arrange
        var securityOptions = new SecurityOptions { EnableIpWhitelist = false };
        var service = CreateService(securityOptions);

        // Act & Assert
        service.IsIpAllowed("192.168.1.100").Should().BeTrue();
        service.IsIpAllowed(IPAddress.Parse("10.0.0.1")).Should().BeTrue();
    }

    [Theory]
    [InlineData("192.168.1.100", "192.168.1.0/24", true)]
    [InlineData("192.168.1.1", "192.168.1.0/24", true)]
    [InlineData("192.168.2.100", "192.168.1.0/24", false)]
    [InlineData("127.0.0.1", "127.0.0.1/32", true)]
    [InlineData("127.0.0.2", "127.0.0.1/32", false)]
    public void IsIpAllowed_WithCidrRange_ReturnsExpectedResult(string testIp, string cidrRange, bool expected)
    {
        // Arrange
        var securityOptions = new SecurityOptions 
        { 
            EnableIpWhitelist = true,
            AllowedIpRanges = new[] { cidrRange }
        };
        var service = CreateService(securityOptions);

        // Act & Assert
        service.IsIpAllowed(testIp).Should().Be(expected);
    }

    [Fact]
    public void IsIpAllowed_WithMultipleRanges_WorksCorrectly()
    {
        // Arrange
        var securityOptions = new SecurityOptions 
        { 
            EnableIpWhitelist = true,
            AllowedIpRanges = new[] { "192.168.1.0/24", "10.0.0.0/8", "127.0.0.1/32" }
        };
        var service = CreateService(securityOptions);

        // Act & Assert
        service.IsIpAllowed("192.168.1.100").Should().BeTrue();
        service.IsIpAllowed("10.0.0.1").Should().BeTrue();
        service.IsIpAllowed("127.0.0.1").Should().BeTrue();
        service.IsIpAllowed("8.8.8.8").Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-ip")]
    public void IsIpAllowed_WithInvalidStringIp_ReturnsFalse(string ipAddress)
    {
        // Arrange
        var securityOptions = new SecurityOptions { EnableIpWhitelist = true };
        var service = CreateService(securityOptions);

        // Act & Assert
        service.IsIpAllowed(ipAddress).Should().BeFalse();
    }

    [Fact]
    public void IsIpAllowed_WithNullIpAddress_ReturnsFalse()
    {
        // Arrange
        var securityOptions = new SecurityOptions { EnableIpWhitelist = true };
        var service = CreateService(securityOptions);

        // Act & Assert
        service.IsIpAllowed((IPAddress)null!).Should().BeFalse();
        service.IsIpAllowed((string)null!).Should().BeFalse();
    }

    [Fact]
    public void IsIpAllowed_WithEmptyAllowedRanges_ReturnsFalse()
    {
        // Arrange
        var securityOptions = new SecurityOptions 
        { 
            EnableIpWhitelist = true,
            AllowedIpRanges = Array.Empty<string>()
        };
        var service = CreateService(securityOptions);

        // Act & Assert
        service.IsIpAllowed("192.168.1.100").Should().BeFalse();
    }

    [Fact]
    public void GetAllowedRanges_ReturnsConfiguredRanges()
    {
        // Arrange
        var expectedRanges = new[] { "192.168.1.0/24", "10.0.0.0/8" };
        var securityOptions = new SecurityOptions { AllowedIpRanges = expectedRanges };
        var service = CreateService(securityOptions);

        // Act & Assert
        service.GetAllowedRanges().Should().BeEquivalentTo(expectedRanges);
    }

    [Theory]
    [InlineData("::1", "::1/128", true)]
    [InlineData("::2", "::1/128", false)]
    [InlineData("2001:db8::1", "2001:db8::/32", true)]
    public void IsIpAllowed_WithIPv6Addresses_ReturnsExpectedResult(string testIp, string cidrRange, bool expected)
    {
        // Arrange
        var securityOptions = new SecurityOptions 
        { 
            EnableIpWhitelist = true,
            AllowedIpRanges = new[] { cidrRange }
        };
        var service = CreateService(securityOptions);

        // Act & Assert
        service.IsIpAllowed(testIp).Should().Be(expected);
    }

    private static IpWhitelistService CreateService(SecurityOptions securityOptions)
    {
        var optionsMonitor = CreateOptionsMonitor(securityOptions);
        var logger = new Mock<ILogger<IpWhitelistService>>();
        return new IpWhitelistService(optionsMonitor, logger.Object);
    }

    private static IOptionsMonitor<SecurityOptions> CreateOptionsMonitor(SecurityOptions securityOptions)
    {
        var mock = new Mock<IOptionsMonitor<SecurityOptions>>();
        mock.Setup(x => x.CurrentValue).Returns(securityOptions);
        return mock.Object;
    }
} 