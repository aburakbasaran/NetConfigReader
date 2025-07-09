using ConfigReader.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConfigReader.Api.Controllers;

/// <summary>
/// Token yönetimi controller (sadece development ortamında)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
    private readonly ITokenGeneratorService _tokenGeneratorService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<TokenController> _logger;

    public TokenController(
        ITokenGeneratorService tokenGeneratorService,
        IWebHostEnvironment environment,
        ILogger<TokenController> logger)
    {
        _tokenGeneratorService = tokenGeneratorService ?? throw new ArgumentNullException(nameof(tokenGeneratorService));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Yeni token üretir (sadece development ortamında)
    /// </summary>
    /// <param name="expiryMinutes">Token'ın geçerlilik süresi (dakika), varsayılan: 1</param>
    /// <returns>Üretilen token bilgisi</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateToken([FromQuery] int expiryMinutes = 1)
    {
        // Sadece development ortamında çalışsın
        if (!_environment.IsDevelopment())
        {
            return NotFound("This endpoint is only available in development environment");
        }

        if (expiryMinutes < 1 || expiryMinutes > 1440) // Max 24 saat
        {
            return BadRequest("Expiry minutes must be between 1 and 1440 (24 hours)");
        }

        try
        {
            var token = await _tokenGeneratorService.GenerateTokenAsync(expiryMinutes);
            var response = new TokenResponse
            {
                Token = token,
                ExpiresIn = expiryMinutes,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                CreatedAt = DateTime.UtcNow,
                Usage = $"curl -H \"X-ConfigReader-Token: {token}\" http://localhost:5000/api/configuration"
            };

            _logger.LogInformation("Token üretildi: {Token}, Expiry: {Expiry} dakika", token, expiryMinutes);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token üretme hatası");
            return StatusCode(500, "Token üretme hatası");
        }
    }

    /// <summary>
    /// Aktif token'ları listeler (sadece development ortamında)
    /// </summary>
    /// <returns>Aktif token listesi</returns>
    [HttpGet("list")]
    [ProducesResponseType(typeof(TokenListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListActiveTokens()
    {
        // Sadece development ortamında çalışsın
        if (!_environment.IsDevelopment())
        {
            return NotFound("This endpoint is only available in development environment");
        }

        try
        {
            var activeTokens = await _tokenGeneratorService.GetActiveTokensAsync();
            var response = new TokenListResponse
            {
                ActiveTokens = activeTokens,
                Count = activeTokens.Count,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Aktif token sayısı: {Count}", activeTokens.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token listesi alma hatası");
            return StatusCode(500, "Token listesi alma hatası");
        }
    }

    /// <summary>
    /// Token'ı iptal eder (sadece development ortamında)
    /// </summary>
    /// <param name="token">İptal edilecek token</param>
    /// <returns>İptal durumu</returns>
    [HttpDelete("revoke")]
    [ProducesResponseType(typeof(RevokeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeToken([FromQuery] string token)
    {
        // Sadece development ortamında çalışsın
        if (!_environment.IsDevelopment())
        {
            return NotFound("This endpoint is only available in development environment");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("Token cannot be empty");
        }

        try
        {
            await _tokenGeneratorService.RevokeTokenAsync(token);
            var response = new RevokeResponse
            {
                Token = token,
                RevokedAt = DateTime.UtcNow,
                Success = true,
                Message = "Token successfully revoked"
            };

            _logger.LogInformation("Token iptal edildi: {Token}", token);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token iptal etme hatası");
            return StatusCode(500, "Token iptal etme hatası");
        }
    }
}

/// <summary>
/// Token response model
/// </summary>
public class TokenResponse
{
    public string Token { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Usage { get; set; } = string.Empty;
}

/// <summary>
/// Token list response model
/// </summary>
public class TokenListResponse
{
    public List<string> ActiveTokens { get; set; } = new();
    public int Count { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Token revoke response model
/// </summary>
public class RevokeResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime RevokedAt { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
} 