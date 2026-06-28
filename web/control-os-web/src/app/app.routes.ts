import { Routes } from '@angular/router';
import { DashboardPageComponent } from './pages/dashboard-page.component';
import { DevicesPageComponent } from './pages/devices-page.component';
import { LogsPageComponent } from './pages/logs-page.component';
import { ScannerPageComponent } from './pages/scanner-page.component';
import { SettingsPageComponent } from './pages/settings-page.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'dashboard', component: DashboardPageComponent },
  { path: 'devices', component: DevicesPageComponent },
  { path: 'scanner', component: ScannerPageComponent },
  { path: 'logs', component: LogsPageComponent },
  { path: 'settings', component: SettingsPageComponent }
];
