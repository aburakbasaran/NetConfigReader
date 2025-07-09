# ConfigReader API Project - Comprehensive Development Prompt

## Project Overview
Create a secure, enterprise-grade Configuration Management API with advanced security features. This system should manage application configurations with multiple security layers, comprehensive testing, and production-ready deployment capabilities.

## Core Requirements

### 1. Project Structure
Create a well-organized project with the following components:
- **API Layer**: RESTful API with proper routing and controllers
- **Service Layer**: Business logic and data processing services
- **Middleware Layer**: Security and request processing middleware
- **Model Layer**: Data transfer objects and configuration models
- **Test Layer**: Comprehensive unit and integration tests
- **Documentation**: API documentation and deployment guides

### 2. API Endpoints

#### Configuration Management
- `GET /api/configuration` - List all configuration items
- `GET /api/configuration/{key}` - Get specific configuration by key
- `POST /api/configuration` - Create new configuration item
- `PUT /api/configuration/{key}` - Update existing configuration
- `DELETE /api/configuration/{key}` - Delete configuration item

#### Token Management
- `POST /api/token/generate` - Generate new access token
- `POST /api/token/validate` - Validate existing token
- `DELETE /api/token/revoke` - Revoke access token

#### System Health
- `GET /api/health` - System health check
- `GET /api/health/detailed` - Detailed system status

### 3. Security Architecture (6-Layer Security Pipeline)

#### Layer 1: IP Whitelist Middleware
- **Purpose**: Network-level access control
- **Features**:
  - CIDR notation support for IPv4 and IPv6
  - Proxy header support (X-Forwarded-For, X-Real-IP)
  - Configuration-based enable/disable
  - Corporate VPN IP range management
- **Logic**: Block requests from non-whitelisted IPs immediately

#### Layer 2: Configuration Toggle Middleware
- **Purpose**: Feature flag for API availability
- **Features**:
  - Environment-based configuration
  - Runtime enable/disable capability
  - Maintenance mode support
- **Logic**: Return 503 Service Unavailable when disabled

#### Layer 3: Sensitive Endpoint Logging Control
- **Purpose**: Prevent sensitive data exposure in logs
- **Features**:
  - Configurable sensitive endpoint patterns
  - Request/response logging control
  - Security audit compliance
- **Logic**: Disable detailed logging for sensitive endpoints

#### Layer 4: Rate Limiting Middleware
- **Purpose**: Prevent API abuse and DoS attacks
- **Features**:
  - Per-IP rate limiting (10 requests/day default)
  - Configurable time windows and limits
  - Different limits for different endpoint categories
  - Memory-based storage with optional Redis support
- **Logic**: Return 429 Too Many Requests when limit exceeded

#### Layer 5: Token Authentication Middleware
- **Purpose**: API access control and user identification
- **Features**:
  - Complex token generation and validation
  - Token expiration management
  - Header-based authentication (Authorization: Bearer)
  - Token revocation capability
- **Logic**: Return 401 Unauthorized for invalid/missing tokens

#### Layer 6: Framework Authentication
- **Purpose**: Final authentication layer using framework features
- **Features**:
  - Integration with application framework's auth system
  - Role-based access control
  - Session management
- **Logic**: Standard framework authentication flow

### 4. Data Models

#### ConfigurationItem
```
{
  "key": "string (required, unique)",
  "value": "string (required)",
  "source": "enum: Database, File, Environment, Cache",
  "isEncrypted": "boolean",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

#### ConfigurationSource
```
{
  "name": "string",
  "type": "enum: Database, File, Environment, Cache",
  "isActive": "boolean",
  "priority": "integer",
  "lastSyncTime": "datetime"
}
```

### 5. Service Layer Implementation

#### IConfigurationService
- Configuration CRUD operations
- Data validation and transformation
- Source management (Database, File, Environment, Cache)
- Encryption/decryption for sensitive values

#### IIpWhitelistService
- CIDR notation parsing and validation
- IP address range checking
- IPv4 and IPv6 support
- Configuration loading and validation

#### IRateLimitService
- Request counting and tracking
- Time window management
- IP-based rate limiting
- Configurable limits per endpoint

#### ITokenAuthenticationService
- Token generation with crypto-secure randomness
- Token validation and expiration checking
- Token revocation and blacklisting
- Header parsing and validation

#### IDataMaskingService
- Production data masking (first 5 + "..." + last 5 characters)
- Configurable masking patterns
- Sensitive field detection
- Environment-aware masking

### 6. Configuration System

#### Development Environment
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ConfigReader.Api": "Debug"
    }
  },
  "SecuritySettings": {
    "ApiEnabled": true,
    "RateLimitEnabled": false,
    "TokenAuthEnabled": false,
    "IpWhitelistEnabled": false,
    "SensitiveLoggingEnabled": true
  },
  "RateLimit": {
    "RequestsPerDay": 1000,
    "TimeWindow": "01:00:00"
  },
  "IpWhitelist": {
    "AllowedIPs": ["127.0.0.1", "::1"],
    "AllowedCIDRs": ["10.0.0.0/8", "192.168.0.0/16"]
  }
}
```

#### Production Environment
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error",
      "ConfigReader.Api": "Warning"
    }
  },
  "SecuritySettings": {
    "ApiEnabled": true,
    "RateLimitEnabled": true,
    "TokenAuthEnabled": true,
    "IpWhitelistEnabled": true,
    "SensitiveLoggingEnabled": false
  },
  "RateLimit": {
    "RequestsPerDay": 10,
    "TimeWindow": "24:00:00"
  },
  "IpWhitelist": {
    "AllowedIPs": ["203.0.113.0"],
    "AllowedCIDRs": ["10.0.0.0/8", "172.16.0.0/12"]
  }
}
```

### 7. Testing Requirements

#### Unit Tests (Minimum 200+ tests)
- **Service Tests**: All service layer methods
- **Middleware Tests**: Each middleware component
- **Controller Tests**: All API endpoints
- **Model Tests**: Data validation and transformation
- **Utility Tests**: Helper functions and extensions

#### Integration Tests
- **API Flow Tests**: End-to-end request processing
- **Security Tests**: Authentication and authorization flows
- **Middleware Pipeline Tests**: Full security pipeline validation
- **Configuration Tests**: Different environment scenarios

#### Test Coverage Requirements
- Minimum 90% code coverage
- All security features thoroughly tested
- Error scenarios and edge cases covered
- Performance and load testing for rate limiting

### 8. Error Handling and Responses

#### Standard Error Response Format
```json
{
  "error": {
    "code": "string",
    "message": "string",
    "timestamp": "datetime",
    "requestId": "string"
  }
}
```

#### HTTP Status Codes
- 200: Success
- 201: Created
- 400: Bad Request
- 401: Unauthorized
- 403: Forbidden
- 404: Not Found
- 409: Conflict
- 429: Too Many Requests
- 500: Internal Server Error
- 503: Service Unavailable

### 9. Middleware Pipeline Order (Critical)
The middleware must be registered in this exact order:
1. IP Whitelist Middleware (First - Network Security)
2. Configuration Toggle Middleware (API Availability)
3. Sensitive Logging Middleware (Audit Control)
4. Rate Limiting Middleware (DoS Protection)
5. Token Authentication Middleware (Access Control)
6. Framework Authentication (Final Auth Layer)

### 10. Production Features

#### Data Masking
- Mask sensitive configuration values in production
- Format: `first5chars...last5chars`
- Configurable masking patterns
- Environment-aware activation

#### Monitoring Integration
- Health check endpoints
- Metrics collection (request count, response times)
- Error tracking and alerting
- Performance monitoring

#### Deployment Support
- Docker containerization
- Kubernetes deployment manifests
- Environment-specific configurations
- Blue-green deployment support

### 11. Documentation Requirements

#### API Documentation
- OpenAPI/Swagger specification
- Endpoint documentation with examples
- Authentication requirements
- Error response examples

#### Security Documentation
- Security architecture overview
- Threat model and mitigations
- Configuration security guidelines
- Compliance and audit information

#### Deployment Documentation
- Installation instructions
- Configuration guidelines
- Monitoring setup
- Troubleshooting guide

### 12. Performance Requirements

#### Response Times
- GET requests: < 100ms
- POST/PUT requests: < 200ms
- Bulk operations: < 500ms

#### Scalability
- Support for horizontal scaling
- Stateless design
- Database connection pooling
- Caching strategies

### 13. Additional Features

#### Logging
- Structured logging with correlation IDs
- Different log levels per environment
- Security event logging
- Performance metrics logging

#### Caching
- In-memory caching for frequently accessed configs
- Cache invalidation strategies
- TTL-based cache expiration

#### Backup and Recovery
- Configuration backup capabilities
- Point-in-time recovery
- Data export/import features

## Implementation Notes

1. **Security First**: Implement all security layers before adding business logic
2. **Test-Driven Development**: Write tests alongside implementation
3. **Configuration Management**: Use environment-based configuration extensively
4. **Error Handling**: Implement comprehensive error handling and logging
5. **Performance**: Design for scalability and performance from the start
6. **Documentation**: Maintain comprehensive documentation throughout development
7. **Code Quality**: Follow language-specific best practices and coding standards

## Success Criteria

- All 6 security layers implemented and working
- 200+ comprehensive tests with 90%+ coverage
- Production-ready configuration management
- Complete API documentation
- Performance benchmarks met
- Security audit compliance
- Deployment automation ready

This prompt provides a complete blueprint for creating a production-ready, secure Configuration Management API in any programming language. Follow the requirements systematically, implementing each layer with proper testing and documentation. 