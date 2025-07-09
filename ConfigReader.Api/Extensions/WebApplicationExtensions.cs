namespace ConfigReader.Api.Extensions;

/// <summary>
/// WebApplication extension methods
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Request pipeline'ı konfigüre eder
    /// </summary>
    /// <param name="app">WebApplication</param>
    /// <returns>WebApplication</returns>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Development environment
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Configuration Reader API V1");
                c.RoutePrefix = string.Empty; // Swagger UI'yi root'da göster
            });
        }

        // Security
        app.UseHttpsRedirection();

        // CORS
        app.UseCors("DefaultPolicy");

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Health checks
        app.MapHealthChecks("/health");

        // Controllers
        app.MapControllers();

        return app;
    }
} 