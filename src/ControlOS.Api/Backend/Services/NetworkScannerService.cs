using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using ControlOS.Api.Backend.Interfaces;
using ControlOS.Api.Backend.Models;
using ControlOS.Api.Backend.Results;

namespace ControlOS.Api.Backend.Services;

public sealed class NetworkScannerService : INetworkScannerService
{
    public Result<string> GetSuggestedSubnet()
    {
        try
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
                    return Result<string>.Success($"{bytes[0]}.{bytes[1]}.{bytes[2]}");
                }
            }

            return Result<string>.Success("192.168.1");
        }
        catch (Exception exception)
        {
            return Result<string>.Failure("network.subnet.failed", exception.Message);
        }
    }

    public async Task<Result<IReadOnlyList<NetworkScanResult>>> ScanSubnetAsync(
        string subnetPrefix,
        int startHost,
        int endHost,
        int timeoutMs,
        int maxConcurrency,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
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

            IReadOnlyList<NetworkScanResult> orderedResults = results
                .OrderBy(result => result.IpAddress, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Result<IReadOnlyList<NetworkScanResult>>.Success(orderedResults);
        }
        catch (ArgumentException exception)
        {
            return Result<IReadOnlyList<NetworkScanResult>>.Failure("network.subnet.invalid", exception.Message);
        }
        catch (Exception exception)
        {
            return Result<IReadOnlyList<NetworkScanResult>>.Failure("network.scan.failed", exception.Message);
        }
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
                MacAddress = TryResolveMacAddress(ipAddress),
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

    private static string TryResolveMacAddress(string ipAddress)
    {
        try
        {
            if (!IPAddress.TryParse(ipAddress, out IPAddress? address) || address.AddressFamily != AddressFamily.InterNetwork)
            {
                return string.Empty;
            }

            byte[] addressBytes = address.GetAddressBytes();
            int destinationIp = BitConverter.ToInt32(addressBytes, 0);
            byte[] macAddressBytes = new byte[6];
            int macAddressLength = macAddressBytes.Length;

            int result = SendARP(destinationIp, 0, macAddressBytes, ref macAddressLength);
            if (result != 0 || macAddressLength <= 0)
            {
                return string.Empty;
            }

            return string.Join(":",
                macAddressBytes
                    .Take(macAddressLength)
                    .Select(value => value.ToString("X2")));
        }
        catch
        {
            return string.Empty;
        }
    }

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(int destIp, int srcIp, byte[] macAddr, ref int physicalAddrLen);
}
