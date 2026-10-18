using System.Net;
using System.Net.Sockets;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;

namespace WebChatAI.Infrastructure.Services;

public class SsrfProtectionService : ISsrfProtectionService
{
    private static readonly HashSet<string> DisallowedHostnames = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "::1",
        "169.254.169.254",
        "metadata.google.internal",
        "metadata.azure.internal",
        "instance-data",
        "kubernetes.default.svc"
    };

    public async Task<ValidateTargetResponseDto> ValidateTargetUrlAsync(string targetUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            return new ValidateTargetResponseDto
            {
                IsValid = false,
                ErrorMessage = "Hedef URL adresi boş olamaz."
            };
        }

        var trimmedUrl = targetUrl.Trim();
        if (!trimmedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !trimmedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Prepend https:// for evaluation if scheme omitted
            trimmedUrl = "https://" + trimmedUrl;
        }

        if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri))
        {
            return new ValidateTargetResponseDto
            {
                IsValid = false,
                ErrorMessage = "Girilen URL formatı geçersiz."
            };
        }

        // 1. Only allow HTTP and HTTPS
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return new ValidateTargetResponseDto
            {
                IsValid = false,
                ErrorMessage = "Yalnızca HTTP ve HTTPS protokolleri desteklenmektedir."
            };
        }

        var host = uri.Host;

        // 2. Reject disallowed hostnames & local domain suffixes
        if (DisallowedHostnames.Contains(host) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".lan", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".home", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidateTargetResponseDto
            {
                IsValid = false,
                Host = host,
                ErrorMessage = "SSRF Koruması: Yerel ana bilgisayarlar (localhost/internal), yerel ağ adları veya bulut metadata adresleri taranamaz."
            };
        }

        // 3. DNS Resolution & IP Address check
        try
        {
            IPAddress[] addresses;
            if (IPAddress.TryParse(host, out var directIp))
            {
                addresses = new[] { directIp };
            }
            else
            {
                addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            }

            if (addresses.Length == 0)
            {
                return new ValidateTargetResponseDto
                {
                    IsValid = false,
                    Host = host,
                    ErrorMessage = "Hedef ana makine adına ait IP adresi çözümlenemedi (DNS hatası)."
                };
            }

            foreach (var ip in addresses)
            {
                if (IsDisallowedIpAddress(ip))
                {
                    return new ValidateTargetResponseDto
                    {
                        IsValid = false,
                        Host = host,
                        ErrorMessage = $"SSRF Koruması: Hedef IP adresi ({ip}) özel ağ, loopback veya kısıtlanmış IP aralığına denk geldiği için test reddedildi."
                    };
                }
            }
        }
        catch (SocketException ex)
        {
            return new ValidateTargetResponseDto
            {
                IsValid = false,
                Host = host,
                ErrorMessage = $"Hedef ana makine adresi çözümlenemedi: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ValidateTargetResponseDto
            {
                IsValid = false,
                Host = host,
                ErrorMessage = $"Hedef doğrulanırken hata oluştu: {ex.Message}"
            };
        }

        // Clean normalized URL without unnecessary query/hash for target root
        var normalizedBuilder = new UriBuilder(uri)
        {
            Fragment = string.Empty
        };

        return new ValidateTargetResponseDto
        {
            IsValid = true,
            Host = host,
            NormalizedUrl = normalizedBuilder.Uri.ToString()
        };
    }

    public static bool IsDisallowedIpAddress(IPAddress address)
    {
        // Unmap IPv4 mapped into IPv6 (e.g. ::ffff:127.0.0.1)
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        // Check Loopback (127.0.0.1 to 127.255.255.255 or ::1)
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        // IPv6 LinkLocal or SiteLocal
        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
        {
            return true;
        }

        var bytes = address.GetAddressBytes();

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // ::/128 (Unspecified) or ::1/128 (Loopback)
            if (address.Equals(IPAddress.IPv6None) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6Loopback))
            {
                return true;
            }

            // fc00::/7 (Unique Local Address - RFC 4193: covers fc00::/8 and fd00::/8)
            if ((bytes[0] & 0xFE) == 0xFC)
            {
                return true;
            }

            // fe80::/10 (Link-Local unicast)
            if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
            {
                return true;
            }

            // ff00::/8 (Multicast)
            if (bytes[0] == 0xFF)
            {
                return true;
            }

            // 2001:db8::/32 (Documentation prefix - RFC 3849)
            if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8)
            {
                return true;
            }

            // 0100::/64 (Discard-Only prefix - RFC 6666)
            if (bytes[0] == 0x01 && bytes[1] == 0x00)
            {
                return true;
            }
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            byte b1 = bytes[0];
            byte b2 = bytes[1];

            // 0.0.0.0/8 (Current network)
            if (b1 == 0) return true;

            // 10.0.0.0/8 (Private RFC 1918)
            if (b1 == 10) return true;

            // 172.16.0.0/12 (Private RFC 1918)
            if (b1 == 172 && (b2 >= 16 && b2 <= 31)) return true;

            // 192.168.0.0/16 (Private RFC 1918)
            if (b1 == 192 && b2 == 168) return true;

            // 169.254.0.0/16 (Link Local & AWS/GCP/Azure Metadata 169.254.169.254)
            if (b1 == 169 && b2 == 254) return true;

            // 100.64.0.0/10 (Shared Address Space / CGNAT)
            if (b1 == 100 && (b2 >= 64 && b2 <= 127)) return true;

            // 192.0.2.0/24 (TEST-NET-1)
            if (b1 == 192 && b2 == 0 && bytes[2] == 2) return true;

            // 198.51.100.0/24 (TEST-NET-2)
            if (b1 == 198 && b2 == 51 && bytes[2] == 100) return true;

            // 203.0.113.0/24 (TEST-NET-3)
            if (b1 == 203 && b2 == 0 && bytes[2] == 113) return true;

            // 224.0.0.0/4 (Multicast)
            if (b1 >= 224 && b1 <= 239) return true;

            // 240.0.0.0/4 (Reserved)
            if (b1 >= 240) return true;

            // 255.255.255.255 (Broadcast)
            if (b1 == 255 && b2 == 255 && bytes[2] == 255 && bytes[3] == 255) return true;
        }

        return false;
    }
}
