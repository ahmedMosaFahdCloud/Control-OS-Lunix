using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Control_OS_Lunix.Backend.Interfaces;
using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;
using Renci.SshNet;

namespace Control_OS_Lunix.Backend.Services;

public sealed class DevicePowerService : IDevicePowerService
{
    public async Task<Result<DevicePowerReport>> GetReportAsync(
        DevicePowerConfig device,
        int pingTimeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var ping = new Ping();
            PingReply reply = await ping.SendPingAsync(device.IpAddress, pingTimeoutSeconds * 1000)
                .WaitAsync(cancellationToken);

            return Result<DevicePowerReport>.Success(new DevicePowerReport
            {
                DeviceId = device.DeviceId,
                Name = device.Name,
                IpAddress = device.IpAddress,
                Status = reply.Status == IPStatus.Success ? DevicePowerStatus.Online : DevicePowerStatus.Offline,
                Message = reply.Status.ToString()
            });
        }
        catch (Exception exception)
        {
            return Result<DevicePowerReport>.Failure("device.report.failed", exception.Message);
        }
    }

    public async Task<Result> SendWakeOnLanAsync(DevicePowerConfig device, CancellationToken cancellationToken = default)
    {
        try
        {
            byte[] packet = BuildMagicPacket(device.MacAddress);
            using var udpClient = new UdpClient { EnableBroadcast = true };
            var endPoint = new IPEndPoint(IPAddress.Parse(device.BroadcastAddress), device.WolPort);
            await udpClient.SendAsync(packet, packet.Length, endPoint).WaitAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception exception)
        {
            return Result.Failure("device.wol.failed", exception.Message);
        }
    }

    public Task<Result> ShutdownAsync(DevicePowerConfig device, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        return RunSshCommandAsync(device, "sudo -n /usr/bin/systemctl poweroff", timeoutSeconds, cancellationToken);
    }

    public Task<Result> RebootAsync(DevicePowerConfig device, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        return RunSshCommandAsync(device, "sudo -n /usr/bin/systemctl reboot", timeoutSeconds, cancellationToken);
    }

    private static Task<Result> RunSshCommandAsync(
        DevicePowerConfig device,
        string command,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            try
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
                    return Result.Failure("device.ssh.connect.failed", "SSH connection failed.");
                }

                using SshCommand sshCommand = sshClient.CreateCommand(command);
                string result = sshCommand.Execute();

                if (sshCommand.ExitStatus != 0)
                {
                    return Result.Failure(
                        "device.ssh.command.failed",
                        $"SSH command failed. Command: {command}. Error: {sshCommand.Error}. Result: {result}");
                }

                sshClient.Disconnect();
                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure("device.ssh.failed", exception.Message);
            }
        }, cancellationToken);
    }

    private static byte[] BuildMagicPacket(string macAddress)
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

        byte[] packet = new byte[6 + (16 * bytes.Length)];
        for (int index = 0; index < 6; index++)
        {
            packet[index] = 0xFF;
        }

        for (int index = 6; index < packet.Length; index += bytes.Length)
        {
            Buffer.BlockCopy(bytes, 0, packet, index, bytes.Length);
        }

        return packet;
    }
}
