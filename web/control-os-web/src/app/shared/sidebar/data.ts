export interface NavItem {
  title: string;
  url: string;
  icon: string;
}

export const data = {
  navMain: [
    { title: 'Dashboard', url: '/dashboard', icon: 'layout-dashboard' },
    { title: 'Devices', url: '/devices', icon: 'monitor' },
    { title: 'Scanner', url: '/scanner', icon: 'radio' },
    { title: 'Logs', url: '/logs', icon: 'logs' },
    { title: 'Settings', url: '/settings', icon: 'settings' },
  ] satisfies NavItem[],
};
