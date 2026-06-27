using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Control_OS_Lunix.Model;

namespace Control_OS_Lunix.Service;

public sealed class NetworkScannerService
{
    public string GetSuggestedSubnet()
    {
        foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            IPInterfaceProperties properties = networkInterface.GetIPProperties();

            foreach (UnicastIPAddressInformation address in properties.UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork ||
                    IPAddress.IsLoopback(address.Address))
                {
                    continue;
                }

                byte[] bytes = address.Address.GetAddressBytes();
                return $"{bytes[0]}.{bytes[1]}.{bytes[2]}";
            }
        }

        return "192.168.1";
    }

    public async Task<IReadOnlyList<NetworkScanResult>> ScanSubnetAsync(
        string subnetPrefix,
        int startHost,
        int endHost,
        int timeoutMs,
        int maxConcurrency,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string normalizedSubnet = NormalizeSubnetPrefix(subnetPrefix);
        int safeStartHost = Math.Clamp(startHost, 1, 254);
        int safeEndHost = Math.Clamp(endHost, safeStartHost, 254);
        int safeTimeoutMs = Math.Clamp(timeoutMs, 100, 10000);
        int safeConcurrency = Math.Clamp(maxConcurrency, 1, 128);
        int totalHosts = safeEndHost - safeStartHost + 1;
        int completed = 0;
        var results = new ConcurrentBag<NetworkScanResult>();
        using var semaphore = new SemaphoreSlim(safeConcurrency, safeConcurrency);

        List<Task> tasks = [];

        for (int host = safeStartHost; host <= safeEndHost; host++)
        {
            await semaphore.WaitAsync(cancellationToken);
            string ipAddress = $"{normalizedSubnet}.{host}";

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    NetworkScanResult result = await ScanHostAsync(ipAddress, safeTimeoutMs, cancellationToken);
                    if (result.IsOnline)
                    {
                        results.Add(result);
                    }
                }
                finally
                {
                    int value = Interlocked.Increment(ref completed);
                    progress?.Report((int)Math.Round((double)value / totalHosts * 100));
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        return results
            .OrderBy(result => result.IpAddress, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static async Task<NetworkScanResult> ScanHostAsync(string ipAddress, int timeoutMs, CancellationToken cancellationToken)
    {
        using var ping = new Ping();

        try
        {
            PingReply reply = await ping.SendPingAsync(ipAddress, timeoutMs).WaitAsync(cancellationToken);
            if (reply.Status != IPStatus.Success)
            {
                return new NetworkScanResult
                {
                    IpAddress = ipAddress,
                    IsOnline = false
                };
            }

            string hostName = string.Empty;

            try
            {
                IPHostEntry hostEntry = await Dns.GetHostEntryAsync(ipAddress, cancellationToken);
                hostName = hostEntry.HostName;
            }
            catch
            {
                hostName = string.Empty;
            }

            return new NetworkScanResult
            {
                IpAddress = ipAddress,
                HostName = hostName,
                IsOnline = true,
                ResponseTimeMs = reply.RoundtripTime
            };
        }
        catch
        {
            return new NetworkScanResult
            {
                IpAddress = ipAddress,
                IsOnline = false
            };
        }
    }

    private static string NormalizeSubnetPrefix(string subnetPrefix)
    {
        string value = subnetPrefix.Trim();
        string[] parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 3 || parts.Any(part => !byte.TryParse(part, out _)))
        {
            throw new ArgumentException("Subnet must use the format 192.168.1", nameof(subnetPrefix));
        }

        return string.Join(".", parts);
    }
}
