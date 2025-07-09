# ConfigReader API

Bu proje, .NET 8 ile geliştirilmiş bir API'dir. Uygulama çeşitli ortamlarda çalışırken ilgili ortamın environment değişkenleri ve appsettings içerisinde yer alan tüm değerleri key-value olarak döner.

## Özellikler

- ✅ Environment değişkenlerini okuma
- ✅ AppSettings değerlerini okuma
- ✅ Tüm configuration değerlerini listeleme
- ✅ Belirli bir key'e göre configuration değeri getirme
- ✅ RESTful API standartlarına uygun
- ✅ Swagger/OpenAPI dokumentasyonu
- ✅ Kapsamlı unit ve integration testler
- ✅ Dependency Injection
- ✅ Logging desteği
- ✅ CORS desteği
- 🔒 **Güvenlik Özellikleri**
  - Rate limiting (günlük 10 istek/endpoint)
  - Token-based authentication
  - Production data masking
  - Sensitive endpoint logging protection
  - Config-based API enable/disable

## Teknolojiler

- .NET 8
- ASP.NET Core Web API
- xUnit (Test framework)
- Moq (Mock framework)
- FluentAssertions (Test assertions)
- Swagger/OpenAPI
- Microsoft.AspNetCore.Mvc.Testing
- System.Security.Cryptography (SHA256 hashing)
- Custom middleware pipeline (Authentication, Rate limiting, etc.)

## Proje Yapısı

```
ConfigReader/
├── ConfigReader.Api/                 # Ana API projesi
│   ├── Controllers/                  # API Controllers
│   │   └── ConfigurationController.cs
│   ├── Services/                     # Business logic
│   │   ├── IConfigurationService.cs
│   │   ├── ConfigurationService.cs
│   │   ├── IRateLimitService.cs
│   │   ├── RateLimitService.cs
│   │   ├── IDataMaskingService.cs
│   │   ├── DataMaskingService.cs
│   │   ├── ITokenAuthenticationService.cs
│   │   └── TokenAuthenticationService.cs
│   ├── Models/                       # Data models
│   │   ├── ConfigurationItem.cs
│   │   └── ConfigReaderApiOptions.cs
│   ├── Middleware/                   # Custom middleware
│   │   ├── RateLimitMiddleware.cs
│   │   ├── TokenAuthenticationMiddleware.cs
│   │   ├── ConfigBasedToggleMiddleware.cs
│   │   └── SensitiveEndpointLoggingMiddleware.cs
│   ├── Extensions/                   # Extension methods
│   │   ├── ServiceCollectionExtensions.cs
│   │   └── WebApplicationExtensions.cs
│   ├── Program.cs                    # Uygulama giriş noktası
│   ├── appsettings.json             # Production ayarları
│   └── appsettings.Development.json  # Development ayarları
├── ConfigReader.Tests/               # Test projesi
│   ├── Controllers/                  # Controller testleri
│   │   └── ConfigurationControllerTests.cs
│   └── Services/                     # Service testleri
│       ├── ConfigurationServiceTests.cs
│       ├── RateLimitServiceTests.cs
│       ├── DataMaskingServiceTests.cs
│       └── TokenAuthenticationServiceTests.cs
├── ConfigReader.sln                  # Solution dosyası
└── README.md                         # Bu dosya
```

## API Endpoints

### 1. Tüm Configuration Değerlerini Getir
```
GET /api/configuration
```
Hem environment değişkenlerini hem de appsettings değerlerini döner.

### 2. Environment Değişkenlerini Getir
```
GET /api/configuration/environment
```
Sadece environment değişkenlerini döner.

### 3. AppSettings Değerlerini Getir
```
GET /api/configuration/appsettings
```
Sadece appsettings değerlerini döner.

### 4. Belirli Bir Key'in Değerini Getir
```
GET /api/configuration/{key}
```
Belirtilen key'e ait configuration değerini döner.

## Response Format

Tüm endpoint'ler aşağıdaki format'ta JSON döner:

```json
{
  "key": "Configuration anahtarı",
  "value": "Configuration değeri",
  "source": "Environment veya AppSettings"
}
```

## 🔒 Güvenlik Özellikleri

### 1. Token-Based Authentication
API, güvenli token-based authentication kullanır:

```bash
# Header ile token gönderme
curl -H "X-ConfigReader-Token: your-token-here" https://localhost:7000/api/configuration
```

**Konfigürasyon:** `appsettings.json`
```json
{
  "ConfigReaderApi": {
    "Security": {
      "RequireAuth": true,
      "ApiTokens": ["your-secure-token-here"]
    }
  }
}
```

### 2. Rate Limiting
- Günlük 10 istek sınırı (endpoint bazında)
- IP adresi bazında takip
- Otomatik cleanup

### 3. Production Data Masking
Production ortamında hassas veriler maskelenir:
- Format: `first5...last5`
- Connection string'ler, API key'ler maskelenir
- Development'ta masking devre dışı

### 4. Sensitive Endpoint Logging
- API response'ları asla loglanmaz
- Sadece temel request bilgileri log'lanır
- Hassas veri sızıntısı önlenir

### 5. Config-Based Toggle
```json
{
  "ConfigReaderApi": {
    "IsEnabled": true  // API'yi tamamen devre dışı bırakır
  }
}
```

## Kurulum ve Çalıştırma

### Gereksinimler
- .NET 8 SDK

### Kurulum
```bash
# Repository'yi klonla
git clone <repository-url>
cd ConfigReader

# Bağımlılıkları yükle
dotnet restore

# Projeyi derle
dotnet build

# Testleri çalıştır
dotnet test
```

### Güvenlik Konfigürasyonu
`appsettings.json` dosyasında güvenlik ayarlarını yapın:

```json
{
  "ConfigReaderApi": {
    "IsEnabled": true,
    "Security": {
      "RequireAuth": true,
      "EnableRateLimit": true,
      "EnableResponseMasking": true,
      "ApiTokens": ["your-secure-token-here"],
      "TokenHeaderName": "X-ConfigReader-Token"
    }
  }
}
```

### Çalıştırma
```bash
# API'yi çalıştır
cd ConfigReader.Api
dotnet run

# Veya belirli bir port'ta çalıştır
dotnet run --urls "http://localhost:5000"
```

### Docker ile Çalıştırma
```bash
# Dockerfile oluştur (opsiyonel)
# Docker image'i build et
# Container'ı çalıştır
```

## Test Çalıştırma

```bash
# Tüm testleri çalıştır
dotnet test

# Detaylı test çıktısı
dotnet test --verbosity normal

# Coverage raporu (opsiyonel)
dotnet test --collect:"XPlat Code Coverage"
```

### Güvenlik Testleri
```bash
# Rate limiting testi
for i in {1..15}; do curl -H "X-ConfigReader-Token: your-token" https://localhost:7000/api/configuration; done

# Authentication testi
curl https://localhost:7000/api/configuration  # 401 Unauthorized
curl -H "X-ConfigReader-Token: invalid-token" https://localhost:7000/api/configuration  # 401 Unauthorized

# API toggle testi
# appsettings.json'da IsEnabled: false yapın ve API'yi yeniden başlatın
curl https://localhost:7000/api/configuration  # 503 Service Unavailable
```

## Swagger/OpenAPI Dokumentasyonu

Uygulama Development ortamında çalışırken Swagger UI'ye erişebilirsiniz:

```
http://localhost:5000/swagger
```

## Ortam Ayarları

### appsettings.json
Production ortamı için temel ayarlar.

### appsettings.Development.json
Development ortamı için özel ayarlar. Production ayarlarını override eder.

## Kütüphane Hazırlığı

Bu proje bir kütüphane haline getirilebilir. Bunun için:

1. `ConfigReader.Api` projesini `ConfigReader.Library` olarak yeniden adlandır
2. `Microsoft.AspNetCore.App` dependency'sini `Microsoft.Extensions.Configuration` ile değiştir
3. Controller'ları kaldır, sadece service'leri ve model'leri bırak
4. NuGet package olarak publish et

## Lisans

MIT License

## Katkıda Bulunma

1. Fork et
2. Feature branch oluştur (`git checkout -b feature/AmazingFeature`)
3. Commit et (`git commit -m 'Add some AmazingFeature'`)
4. Push et (`git push origin feature/AmazingFeature`)
5. Pull Request oluştur

## Geliştirici Notları

- Tüm kod .NET 8 standartlarına uygun yazılmıştır
- Dependency Injection pattern kullanılmıştır
- Logging Microsoft.Extensions.Logging ile yapılmıştır
- Error handling tüm endpoint'lerde mevcuttur
- Unit testler ve integration testler eklenmiştir
- XML documentation desteği mevcuttur # NetConfigReader
# NetConfigReader
# NetConfigReader
