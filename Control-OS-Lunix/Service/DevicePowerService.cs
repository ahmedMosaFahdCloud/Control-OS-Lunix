using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Control_OS_Lunix.Model;
using Renci.SshNet;

namespace Control_OS_Lunix.Service;

public sealed class DevicePowerService
{
    public async Task<DevicePowerReport> GetReportAsync(
        DevicePowerConfig device,
        int pingTimeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var ping = new Ping();

            PingReply reply = await ping.SendPingAsync(device.IpAddress, pingTimeoutSeconds * 1000)
                .WaitAsync(cancellationToken);

            return new DevicePowerReport
            {
                DeviceId = device.DeviceId,
                Name = device.Name,
                IpAddress = device.IpAddress,
                Status = reply.Status == IPStatus.Success ? DevicePowerStatus.Online : DevicePowerStatus.Offline,
                Message = reply.Status.ToString()
            };
        }
        catch (Exception exception)
        {
            return new DevicePowerReport
            {
                DeviceId = device.DeviceId,
                Name = device.Name,
                IpAddress = device.IpAddress,
                Status = DevicePowerStatus.Unknown,
                Message = exception.Message
            };
        }
    }

    public async Task SendWakeOnLanAsync(DevicePowerConfig device, CancellationToken cancellationToken = default)
    {
        byte[] packet = BuildMagicPacket(device.MacAddress);

        using var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        var endPoint = new IPEndPoint(IPAddress.Parse(device.BroadcastAddress), device.WolPort);
        await udpClient.SendAsync(packet, packet.Length, endPoint).WaitAsync(cancellationToken);
    }

    public Task ShutdownAsync(DevicePowerConfig device, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        return RunSshCommandAsync(device, "sudo -n /usr/bin/systemctl poweroff", timeoutSeconds, cancellationToken);
    }

    public Task RebootAsync(DevicePowerConfig device, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        return RunSshCommandAsync(device, "sudo -n /usr/bin/systemctl reboot", timeoutSeconds, cancellationToken);
    }

    private static Task RunSshCommandAsync(
        DevicePowerConfig device,
        string command,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            using var sshClient = new SshClient(
                string.IsNullOrWhiteSpace(device.SshHost) ? device.IpAddress : device.SshHost,
                device.SshPort,
                device.SshUsername,
                device.SshPassword);

            sshClient.ConnectionInfo.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            sshClient.Connect();

            if (!sshClient.IsConnected)
            {
                throw new InvalidOperationException("SSH connection failed.");
            }

            using SshCommand sshCommand = sshClient.CreateCommand(command);
            string result = sshCommand.Execute();

            if (sshCommand.ExitStatus != 0)
            {
                throw new InvalidOperationException(
                    $"SSH command failed. Command: {command}. Error: {sshCommand.Error}. Result: {result}");
            }

            sshClient.Disconnect();
        }, cancellationToken);
    }

    private static byte[] BuildMagicPacket(string macAddress)
    {
        byte[] macBytes = ParseMacAddress(macAddress);
        byte[] packet = new byte[6 + (16 * macBytes.Length)];

        for (int index = 0; index < 6; index++)
        {
            packet[index] = 0xFF;
        }

        for (int index = 6; index < packet.Length; index += macBytes.Length)
        {
            Buffer.BlockCopy(macBytes, 0, packet, index, macBytes.Length);
        }

        return packet;
    }

    private static byte[] ParseMacAddress(string macAddress)
    {
        string cleanMac = new string(macAddress.Where(Uri.IsHexDigit).ToArray());

        if (cleanMac.Length != 12)
        {
            throw new ArgumentException("Invalid MAC address.", nameof(macAddress));
        }

        byte[] bytes = new byte[6];

        for (int index = 0; index < 6; index++)
        {
            bytes[index] = Convert.ToByte(cleanMac.Substring(index * 2, 2), 16);
        }

        return bytes;
    }
}
