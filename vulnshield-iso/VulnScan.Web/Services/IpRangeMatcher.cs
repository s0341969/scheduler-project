using System.Net;
using System.Net.Sockets;

namespace VulnScan.Web.Services;

internal static class IpRangeMatcher
{
    public static bool Contains(string rangeExpression, IPAddress target)
    {
        if (string.IsNullOrWhiteSpace(rangeExpression) || target.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        if (IPAddress.TryParse(rangeExpression, out var singleIp))
        {
            return singleIp.Equals(target);
        }

        if (rangeExpression.Contains('/'))
        {
            return MatchCidr(rangeExpression, target);
        }

        if (rangeExpression.Contains('-'))
        {
            return MatchRange(rangeExpression, target);
        }

        return false;
    }

    private static bool MatchCidr(string cidr, IPAddress target)
    {
        var parts = cidr.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var networkIp) || !int.TryParse(parts[1], out var prefixLength))
        {
            return false;
        }

        var networkBytes = networkIp.GetAddressBytes();
        var targetBytes = target.GetAddressBytes();
        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var index = 0; index < fullBytes; index += 1)
        {
            if (networkBytes[index] != targetBytes[index])
            {
                return false;
            }
        }

        if (remainingBits == 0)
        {
            return true;
        }

        var mask = (byte)~(255 >> remainingBits);
        return (networkBytes[fullBytes] & mask) == (targetBytes[fullBytes] & mask);
    }

    private static bool MatchRange(string rangeExpression, IPAddress target)
    {
        var parts = rangeExpression.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 ||
            !IPAddress.TryParse(parts[0], out var startIp) ||
            !IPAddress.TryParse(parts[1], out var endIp))
        {
            return false;
        }

        var targetValue = ToUInt32(target);
        return targetValue >= ToUInt32(startIp) && targetValue <= ToUInt32(endIp);
    }

    private static uint ToUInt32(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToUInt32(bytes, 0);
    }
}
