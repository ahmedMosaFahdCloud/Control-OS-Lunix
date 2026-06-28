export const API = {
  TIMEOUT_MS: 15_000,
  DEFAULT_LOG_LINES: 200,
  DASHBOARD_LOG_LINES: 20,
  DASHBOARD_REFRESH_MS: 10_000,
  LOGS_REFRESH_MS: 5_000,
  RECENT_LOG_COUNT: 8,
  SKELETON_ROWS: 4,
  SKELETON_SIX: [1, 2, 3, 4, 5, 6],
  SKELETON_FOUR: [1, 2, 3, 4],
  SCAN_LINE_OPTIONS: [50, 100, 200, 500],
  SCAN_DEFAULT_LINES: 100,
} as const;

export const DEFAULTS = {
  BROADCAST_ADDRESS: '255.255.255.255',
  WOL_PORT: 9,
  SSH_PORT: 22,
  SSH_USERNAME: '',
  SSH_PASSWORD: '',
  OS_TYPE: 'Linux' as const,
  TIMEOUT_SECONDS: 15,
  RETRY_COUNT: 1,
  AUTO_START: true,
  AUTO_SHUTDOWN: true,
  MANUAL_CONTROL: true,
  IS_ACTIVE: true,
  DESCRIPTION: '',
  NAME: '',
  IP_ADDRESS: '',
  MAC_ADDRESS: '',
  SSH_HOST: '',
  SUBNET_PREFIX: '192.168.1',
  SCAN_START_HOST: 1,
  SCAN_END_HOST: 40,
  SCAN_TIMEOUT_MS: 700,
  SCAN_MAX_CONCURRENCY: 24,
  SETTINGS_DELAY_MS: 1000,
  SETTINGS_PING_S: 5,
  SETTINGS_SSH_S: 10,
  SETTINGS_RETRY: 1,
  SETTINGS_LOG_DAYS: 30,
  SETTINGS_CONFIRM_SHUTDOWN: true,
  SETTINGS_ENABLE_LOGS: true,
  SETTINGS_AUTO_START_BOOT: true,
  SETTINGS_AUTO_SHUTDOWN_STOP: true,
} as const;

export const STATUS = {
  ONLINE: 'Online',
  OFFLINE: 'Offline',
  STARTING: 'Starting',
  SHUTTING_DOWN: 'ShuttingDown',
  REBOOTING: 'Rebooting',
  UNKNOWN: 'Unknown',
  ERROR: 'Error',
} as const;

export const OPERATION = {
  START: 'Start',
  SHUTDOWN: 'Shutdown',
  REBOOT: 'Reboot',
} as const;

export const OS = {
  LINUX: 'Linux',
  WINDOWS: 'Windows',
} as const;

export const STATUS_BADGE: Record<string, string> = {
  [STATUS.ONLINE]: 'bg-emerald-500/10 text-emerald-700',
  [STATUS.OFFLINE]: 'bg-destructive/10 text-destructive',
  [STATUS.UNKNOWN]: 'bg-warning/10 text-warning',
};

export const STATUS_DOT: Record<string, string> = {
  [STATUS.ONLINE]: 'bg-success',
  [STATUS.OFFLINE]: 'bg-destructive',
  [STATUS.UNKNOWN]: 'bg-warning',
};

export const OS_ICON: Record<string, string> = {
  [OS.WINDOWS]: 'monitor',
  [OS.LINUX]: 'cpu',
};

export const TAB = {
  ALL: 'all',
  PASSWORD: 'password',
  NO_PASSWORD: 'nopassword',
} as const;

export const ERROR_MSGS = {
  OFFLINE: 'Cannot reach the API server. Check if the backend is running.',
  LOAD_DASHBOARD: 'Failed to load dashboard',
  LOAD_DEVICES: 'Failed to load devices',
  LOAD_LOGS: 'Unable to load logs from the API.',
  LOAD_SETTINGS: 'Failed to load settings.',
  SAVE_DEVICE: 'Failed to save device.',
  DELETE_DEVICE: 'Failed to delete device.',
  BACKUP: 'Backup failed',
  RESTORE: 'Restore failed.',
  SCAN: 'Scan failed.',
  SAVE_SETTINGS: 'Failed to save settings.',
} as const;
