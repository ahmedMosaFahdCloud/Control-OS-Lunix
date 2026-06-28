using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ControlOS.Api.Features.Shared;
using ControlOS.Api.Features.Shared.Models;
using Renci.SshNet;

namespace ControlOS.Api.Infrastructure.Services;

public sealed class DevicePowerService
{
    private const string InteractiveAuthenticationMessage = "interactive authentication is required";
    private const string PasswordRequiredMessage = "a password is required";
    private const string ConnectionClosedMessage = "connection was closed";
    private const string ConnectionLostMessage = "connection lost";
    private const string SocketClosedMessage = "socket is closed";

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
        return RunPrivilegedPowerCommandAsync(device, "/usr/bin/systemctl poweroff", timeoutSeconds, cancellationToken);
    }

    public Task<Result> RebootAsync(DevicePowerConfig device, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        return RunPrivilegedPowerCommandAsync(device, "/usr/bin/systemctl reboot", timeoutSeconds, cancellationToken);
    }

    private static Task<Result> RunPrivilegedPowerCommandAsync(
        DevicePowerConfig device,
        string privilegedCommand,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        return RunSshCommandAsync(
            device,
            BuildDetachedSudoCommand(privilegedCommand),
            timeoutSeconds,
            cancellationToken,
            allowConnectionDropAsSuccess: true,
            fallbackCommandFactory: password => BuildDetachedSudoPasswordCommand(password, privilegedCommand));
    }

    private static Task<Result> RunSshCommandAsync(
        DevicePowerConfig device,
        string command,
        int timeoutSeconds,
        CancellationToken cancellationToken,
        bool allowConnectionDropAsSuccess = false,
        Func<string, string>? fallbackCommandFactory = null)
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

                SshCommandResult commandResult = ExecuteCommand(sshClient, command);
                if (commandResult.ExitStatus == 0)
                {
                    sshClient.Disconnect();
                    return Result.Success();
                }

                if (CanRetryWithSudoPassword(device, command, commandResult))
                {
                    string fallbackCommand = fallbackCommandFactory is null
                        ? BuildSudoPasswordCommand(device.SshPassword, command)
                        : fallbackCommandFactory(device.SshPassword);
                    SshCommandResult fallbackResult = ExecuteCommand(sshClient, fallbackCommand);

                    if (fallbackResult.ExitStatus == 0 || (allowConnectionDropAsSuccess && IsExpectedConnectionDrop(fallbackResult)))
                    {
                        sshClient.Disconnect();
                        return Result.Success();
                    }

                    return Result.Failure(
                        "device.ssh.command.failed",
                        $"SSH command failed after sudo password fallback. Command: {command}. Error: {fallbackResult.Error}. Result: {fallbackResult.Output}");
                }

                return Result.Failure(
                    "device.ssh.command.failed",
                    $"SSH command failed. Command: {command}. Error: {commandResult.Error}. Result: {commandResult.Output}");
            }
            catch (Exception exception)
            {
                if (allowConnectionDropAsSuccess && IsExpectedConnectionDrop(exception.Message))
                {
                    return Result.Success();
                }

                return Result.Failure("device.ssh.failed", exception.Message);
            }
        }, cancellationToken);
    }

    private static bool CanRetryWithSudoPassword(DevicePowerConfig device, string command, SshCommandResult commandResult)
    {
        if (string.IsNullOrWhiteSpace(device.SshPassword))
        {
            return false;
        }

        if (!command.Contains("sudo -n ", StringComparison.Ordinal))
        {
            return false;
        }

        string combined = $"{commandResult.Error} {commandResult.Output}".ToLowerInvariant();
        return combined.Contains(InteractiveAuthenticationMessage, StringComparison.Ordinal) ||
               combined.Contains(PasswordRequiredMessage, StringComparison.Ordinal);
    }

    private static string BuildSudoPasswordCommand(string password, string command)
    {
        string privilegedCommand = command.Replace("sudo -n ", string.Empty, StringComparison.Ordinal);
        string escapedPassword = EscapeSingleQuotedShellValue(password);
        return $"printf '%s\\n' '{escapedPassword}' | sudo -S -p '' {privilegedCommand}";
    }

    private static string BuildDetachedSudoCommand(string privilegedCommand)
    {
        return $"sudo -n sh -c 'nohup {privilegedCommand} >/dev/null 2>&1 &'";
    }

    private static string BuildDetachedSudoPasswordCommand(string password, string privilegedCommand)
    {
        string escapedPassword = EscapeSingleQuotedShellValue(password);
        return $"printf '%s\\n' '{escapedPassword}' | sudo -S -p '' sh -c 'nohup {privilegedCommand} >/dev/null 2>&1 &'";
    }

    private static string EscapeSingleQuotedShellValue(string value)
    {
        return value.Replace("'", "'\"'\"'", StringComparison.Ordinal);
    }

    private static bool IsExpectedConnectionDrop(SshCommandResult result)
    {
        return IsExpectedConnectionDrop($"{result.Error} {result.Output}");
    }

    private static bool IsExpectedConnectionDrop(string message)
    {
        string value = message.ToLowerInvariant();
        return value.Contains(ConnectionClosedMessage, StringComparison.Ordinal) ||
               value.Contains(ConnectionLostMessage, StringComparison.Ordinal) ||
               value.Contains(SocketClosedMessage, StringComparison.Ordinal);
    }

    private static SshCommandResult ExecuteCommand(SshClient sshClient, string commandText)
    {
        using SshCommand sshCommand = sshClient.CreateCommand(commandText);
        string output = sshCommand.Execute();

        return new SshCommandResult(
            sshCommand.ExitStatus ?? -1,
            sshCommand.Error ?? string.Empty,
            output ?? string.Empty);
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

    private sealed record SshCommandResult(int ExitStatus, string Error, string Output);
}
