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

## Teknolojiler

- .NET 8
- ASP.NET Core Web API
- xUnit (Test framework)
- Moq (Mock framework)
- FluentAssertions (Test assertions)
- Swagger/OpenAPI
- Microsoft.AspNetCore.Mvc.Testing

## Proje Yapısı

```
ConfigReader/
├── ConfigReader.Api/                 # Ana API projesi
│   ├── Controllers/                  # API Controllers
│   │   └── ConfigurationController.cs
│   ├── Services/                     # Business logic
│   │   ├── IConfigurationService.cs
│   │   └── ConfigurationService.cs
│   ├── Models/                       # Data models
│   │   └── ConfigurationItem.cs
│   ├── Program.cs                    # Uygulama giriş noktası
│   ├── appsettings.json             # Production ayarları
│   └── appsettings.Development.json  # Development ayarları
├── ConfigReader.Tests/               # Test projesi
│   ├── Controllers/                  # Controller testleri
│   │   └── ConfigurationControllerTests.cs
│   └── Services/                     # Service testleri
│       └── ConfigurationServiceTests.cs
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
