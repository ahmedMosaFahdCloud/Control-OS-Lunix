import { OPERATION, OS, STATUS } from './constants';

export type DeviceOperatingSystemType = typeof OS.LINUX | typeof OS.WINDOWS;

export type DevicePowerStatus =
  | typeof STATUS.ONLINE
  | typeof STATUS.OFFLINE
  | typeof STATUS.STARTING
  | typeof STATUS.SHUTTING_DOWN
  | typeof STATUS.REBOOTING
  | typeof STATUS.UNKNOWN
  | typeof STATUS.ERROR;

export type DevicePowerOperation = typeof OPERATION.START | typeof OPERATION.SHUTDOWN | typeof OPERATION.REBOOT;

export interface Device {
  deviceId: string;
  name: string;
  ipAddress: string;
  macAddress: string;
  broadcastAddress: string;
  wolPort: number;
  sshHost: string;
  sshPort: number;
  sshUsername: string;
  hasSshPassword: boolean;
  operatingSystemType: DeviceOperatingSystemType;
  autoStartEnabled: boolean;
  autoShutdownEnabled: boolean;
  manualControlEnabled: boolean;
  timeoutSeconds: number;
  retryCount: number;
  description: string;
  isActive: boolean;
  lastKnownStatus: DevicePowerStatus;
  lastOperationSummary: string;
  createdDateUtc: string;
  lastUpdatedDateUtc: string;
}

export interface DeviceUpsertRequest {
  name: string;
  ipAddress: string;
  macAddress: string;
  broadcastAddress: string;
  wolPort: number;
  sshHost: string;
  sshPort: number;
  sshUsername: string;
  sshPassword: string;
  operatingSystemType: DeviceOperatingSystemType;
  autoStartEnabled: boolean;
  autoShutdownEnabled: boolean;
  manualControlEnabled: boolean;
  timeoutSeconds: number;
  retryCount: number;
  description: string;
  isActive: boolean;
}

export interface DashboardSummary {
  totalDevices: number;
  onlineDevices: number;
  activeDevices: number;
  lastAction: string;
}

export interface DashboardResponse {
  summary: DashboardSummary;
  devices: Device[];
  recentLogs: string[];
}

export interface GlobalSettings {
  autoStartDevicesOnControllerBoot: boolean;
  autoShutdownDevicesOnControllerShutdown: boolean;
  delayBetweenCommandsMs: number;
  pingTimeoutSeconds: number;
  sshTimeoutSeconds: number;
  retryCount: number;
  defaultWolPort: number;
  defaultBroadcastAddress: string;
  enableLogs: boolean;
  logRetentionDays: number;
  confirmManualShutdown: boolean;
}

export interface LogsResponse {
  lines: string[];
}

export interface NetworkScanRequest {
  subnetPrefix: string;
  startHost: number;
  endHost: number;
  timeoutMs: number;
  maxConcurrency: number;
}

export interface NetworkScanResult {
  ipAddress: string;
  hostName: string;
  macAddress: string;
  isOnline: boolean;
  responseTimeMs: number;
  summary: string;
}

export interface OperationResponse {
  deviceId: string;
  deviceName: string;
  operation: DevicePowerOperation;
  isSuccess: boolean;
  hasWarning: boolean;
  message: string;
  statusAfterOperation: DevicePowerStatus;
}

export interface BackupResponse {
  archivePath: string;
  message: string;
}
