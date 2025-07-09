using System.Net;
using Microsoft.Extensions.Options;
using ConfigReader.Api.Models;

namespace ConfigReader.Api.Services;

public class IpWhitelistService : IIpWhitelistService
{
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<IpWhitelistService> _logger;
    private readonly List<(IPAddress Network, int PrefixLength)> _allowedNetworks;

    public IpWhitelistService(IOptionsMonitor<SecurityOptions> securityOptions, ILogger<IpWhitelistService> logger)
    {
        _securityOptions = securityOptions?.CurrentValue ?? throw new ArgumentNullException(nameof(securityOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _allowedNetworks = new List<(IPAddress, int)>();

        InitializeAllowedNetworks();
        
        // Options değişikliklerini dinle
        securityOptions.OnChange(options =>
        {
            _allowedNetworks.Clear();
            InitializeAllowedNetworks();
            _logger.LogInformation("IP whitelist configuration reloaded with {Count} ranges", _allowedNetworks.Count);
        });
    }

    public bool IsIpAllowed(IPAddress ipAddress)
    {
        if (ipAddress == null)
        {
            _logger.LogWarning("Null IP address provided for whitelist check");
            return false;
        }

        if (!IsWhitelistEnabled())
        {
            _logger.LogDebug("IP whitelist is disabled, allowing all IPs");
            return true;
        }

        var isAllowed = _allowedNetworks.Any(network => IsIpInNetwork(ipAddress, network.Network, network.PrefixLength));
        
        if (!isAllowed)
        {
            _logger.LogWarning("IP address {IpAddress} is not in whitelist", ipAddress);
        }
        else
        {
            _logger.LogDebug("IP address {IpAddress} is allowed", ipAddress);
        }

        return isAllowed;
    }

    public bool IsIpAllowed(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            _logger.LogWarning("Empty IP address string provided for whitelist check");
            return false;
        }

        if (!IPAddress.TryParse(ipAddress, out var parsedIp))
        {
            _logger.LogWarning("Invalid IP address format: {IpAddress}", ipAddress);
            return false;
        }

        return IsIpAllowed(parsedIp);
    }

    public IEnumerable<string> GetAllowedRanges()
    {
        return _securityOptions.AllowedIpRanges ?? Enumerable.Empty<string>();
    }

    public bool IsWhitelistEnabled()
    {
        return _securityOptions.EnableIpWhitelist;
    }

    private void InitializeAllowedNetworks()
    {
        if (_securityOptions.AllowedIpRanges == null)
        {
            _logger.LogWarning("No IP ranges configured in AllowedIpRanges");
            return;
        }

        foreach (var range in _securityOptions.AllowedIpRanges)
        {
            try
            {
                if (TryParseCidr(range, out var network, out var prefixLength))
                {
                    _allowedNetworks.Add((network, prefixLength));
                    _logger.LogDebug("Added IP range to whitelist: {Range}", range);
                }
                else
                {
                    _logger.LogError("Invalid CIDR format: {Range}", range);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing IP range: {Range}", range);
            }
        }

        _logger.LogInformation("IP whitelist initialized with {Count} ranges", _allowedNetworks.Count);
        
        if (!IsWhitelistEnabled())
        {
            _logger.LogWarning("IP whitelist is DISABLED - all IPs are allowed. Enable in production for security.");
        }
    }

    private static bool TryParseCidr(string cidr, out IPAddress network, out int prefixLength)
    {
        network = null!;
        prefixLength = 0;

        if (string.IsNullOrWhiteSpace(cidr))
            return false;

        var parts = cidr.Split('/');
        if (parts.Length != 2)
            return false;

        if (!IPAddress.TryParse(parts[0], out network))
            return false;

        if (!int.TryParse(parts[1], out prefixLength))
            return false;

        // IPv4 için 0-32, IPv6 için 0-128 kontrol
        var maxPrefix = network.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        if (prefixLength < 0 || prefixLength > maxPrefix)
            return false;

        return true;
    }

    private static bool IsIpInNetwork(IPAddress ipAddress, IPAddress networkAddress, int prefixLength)
    {
        if (ipAddress.AddressFamily != networkAddress.AddressFamily)
            return false;

        var ipBytes = ipAddress.GetAddressBytes();
        var networkBytes = networkAddress.GetAddressBytes();

        if (ipBytes.Length != networkBytes.Length)
            return false;

        var bytesToCheck = prefixLength / 8;
        var bitsToCheck = prefixLength % 8;

        // Tam byte'ları kontrol et
        for (int i = 0; i < bytesToCheck; i++)
        {
            if (ipBytes[i] != networkBytes[i])
                return false;
        }

        // Kalan bitleri kontrol et
        if (bitsToCheck > 0 && bytesToCheck < ipBytes.Length)
        {
            var mask = (byte)(0xFF << (8 - bitsToCheck));
            if ((ipBytes[bytesToCheck] & mask) != (networkBytes[bytesToCheck] & mask))
                return false;
        }

        return true;
    }
} 