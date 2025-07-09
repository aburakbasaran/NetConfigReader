using ConfigReader.Api.Services;
using System.Reflection;
using System.Text.Json.Serialization;

namespace ConfigReader.Api.Extensions;

/// <summary>
/// ServiceCollection extension methods
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Servisleri konfigüre eder
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="environment">Host environment</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection ConfigureServices(
        this IServiceCollection services, 
        IConfiguration configuration, 
        IWebHostEnvironment environment)
    {
        // Core services
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // API documentation
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
            { 
                Title = "Configuration Reader API", 
                Version = "v1",
                Description = "Environment ve AppSettings değerlerini okumak için API"
            });
            
            // XML yorumları için
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        // Health checks
        services.AddHealthChecks();

        // Application services
        services.AddScoped<IConfigurationService, ConfigurationService>();

        // CORS yapılandırması - Production için güvenli
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                if (environment.IsDevelopment())
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
                else
                {
                    policy.WithOrigins("https://localhost:7000", "https://yourdomain.com")
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
            });
        });

        return services;
    }
} 