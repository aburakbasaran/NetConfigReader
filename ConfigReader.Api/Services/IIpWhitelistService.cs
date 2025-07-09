using System.Net;

namespace ConfigReader.Api.Services;

public interface IIpWhitelistService
{
    bool IsIpAllowed(IPAddress ipAddress);
    bool IsIpAllowed(string ipAddress);
    IEnumerable<string> GetAllowedRanges();
    bool IsWhitelistEnabled();
} 