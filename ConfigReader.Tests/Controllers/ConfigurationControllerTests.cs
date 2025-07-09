using ConfigReader.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConfigReader.Tests.Controllers;

/// <summary>
/// ConfigurationController integration testleri
/// </summary>
public sealed class ConfigurationControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string TestToken = "test-token-12345678901234567890123456789012";

    public ConfigurationControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Test için authentication'ı disable et
                services.Configure<ConfigReader.Api.Models.ConfigReaderApiOptions>(options =>
                {
                    options.Security.RequireAuth = false;
                });
            });
        });
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    [Fact]
    public async Task GetAllConfigurations_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/configuration");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        var configurations = JsonSerializer.Deserialize<ConfigurationItem[]>(content, _jsonOptions);
        
        configurations.Should().NotBeNull();
        configurations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllConfigurations_ReturnsCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/configuration");
        
        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetConfiguration_ValidKey_ReturnsConfiguration()
    {
        // Arrange
        var key = "Logging:LogLevel:Default";
        
        // Act
        var response = await _client.GetAsync($"/api/configuration/{key}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var configuration = await response.Content.ReadFromJsonAsync<ConfigurationItem>(_jsonOptions);
        configuration.Should().NotBeNull();
        configuration!.Key.Should().Be(key);
        configuration.Value.Should().NotBeNull();
        configuration.Source.Should().BeOneOf(ConfigurationSource.Environment, ConfigurationSource.AppSettings);
    }

    [Fact]
    public async Task GetConfiguration_InvalidKey_ReturnsNotFound()
    {
        // Arrange
        var key = "NonExistent:Key";
        
        // Act
        var response = await _client.GetAsync($"/api/configuration/{key}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConfiguration_EmptyKey_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/configuration/%20"); // URL encoded space
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetEnvironmentVariables_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/configuration/environment");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        var configurations = JsonSerializer.Deserialize<ConfigurationItem[]>(content, _jsonOptions);
        
        configurations.Should().NotBeNull();
        configurations.Should().NotBeEmpty();
        configurations.Should().OnlyContain(x => x.Source == ConfigurationSource.Environment);
    }

    [Fact]
    public async Task GetAppSettings_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/configuration/appsettings");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        var configurations = JsonSerializer.Deserialize<ConfigurationItem[]>(content, _jsonOptions);
        
        configurations.Should().NotBeNull();
        configurations.Should().OnlyContain(x => x.Source == ConfigurationSource.AppSettings);
    }

    [Fact]
    public async Task GetAppSettings_ContainsExpectedValues()
    {
        // Act
        var response = await _client.GetAsync("/api/configuration/appsettings");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var configurations = await response.Content.ReadFromJsonAsync<ConfigurationItem[]>(_jsonOptions);
        configurations.Should().NotBeNull();
        
        // appsettings.json dosyasında tanımlı değerleri kontrol et
        configurations.Should().Contain(x => x.Key == "AllowedHosts" && x.Value == "*");
        configurations.Should().Contain(x => x.Key.StartsWith("Logging:"));
        configurations.Should().Contain(x => x.Key.StartsWith("Application:"));
    }

    [Fact]
    public async Task GetEnvironmentVariables_ContainsSystemVariables()
    {
        // Act
        var response = await _client.GetAsync("/api/configuration/environment");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var configurations = await response.Content.ReadFromJsonAsync<ConfigurationItem[]>(_jsonOptions);
        configurations.Should().NotBeNull();
        
        // Sistem environment değişkenlerini kontrol et
        configurations.Should().Contain(x => x.Key == "PATH");
        configurations.Should().OnlyContain(x => x.Source == ConfigurationSource.Environment);
    }

    [Fact]
    public async Task GetAllConfigurations_ContainsBothSources()
    {
        // Act
        var response = await _client.GetAsync("/api/configuration");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var configurations = await response.Content.ReadFromJsonAsync<ConfigurationItem[]>(_jsonOptions);
        configurations.Should().NotBeNull();
        
        // Hem Environment hem de AppSettings kaynaklarını içermeli
        configurations.Should().Contain(x => x.Source == ConfigurationSource.Environment);
        configurations.Should().Contain(x => x.Source == ConfigurationSource.AppSettings);
    }

    [Fact]
    public async Task ApiEndpoints_ReturnCorrectResponseHeaders()
    {
        // Arrange
        var endpoints = new[]
        {
            "/api/configuration",
            "/api/configuration/environment",
            "/api/configuration/appsettings"
        };
        
        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await _client.GetAsync(endpoint);
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }
} 