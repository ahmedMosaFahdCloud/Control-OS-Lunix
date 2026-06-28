import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize, interval } from 'rxjs';
import { ControlApiService } from '../core/control-api.service';
import { DashboardResponse } from '../core/api.models';
import { IconComponent } from '../shared/components/icon.component';
import { ToastService } from '../shared/services/toast.service';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmCard, HlmCardHeader, HlmCardTitle, HlmCardDescription, HlmCardContent } from '@spartan-ng/helm/card';
import { HlmTableContainer, HlmTable, HlmTHead, HlmTBody, HlmTr, HlmTh, HlmTd } from '@spartan-ng/helm/table';
import { HlmSkeleton } from '@spartan-ng/helm/skeleton';
import { HlmSwitch } from '@spartan-ng/helm/switch';
import { HlmProgressImports } from '@spartan-ng/helm/progress';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [DatePipe, RouterLink, IconComponent,
    HlmButton, HlmBadge, HlmSwitch, HlmSkeleton,
    HlmCard, HlmCardHeader, HlmCardTitle, HlmCardDescription, HlmCardContent,
    HlmTableContainer, HlmTable, HlmTHead, HlmTBody, HlmTr, HlmTh, HlmTd,
    ...HlmProgressImports],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 class="text-2xl font-semibold tracking-tight">Dashboard</h1>
          <p class="text-muted-foreground mt-1 text-sm">Real-time overview of managed devices and system activity.</p>
        </div>
        <div class="flex items-center gap-3">
          <label class="flex items-center gap-2 text-sm text-muted-foreground">
            <hlm-switch [checked]="autoRefresh()" (checkedChange)="autoRefresh.set($event)" />
            Auto-refresh
          </label>
          <button hlmBtn variant="outline" (click)="load()">
            <app-icon name="refresh-cw" size="16" [class.animate-spin]="isBusy()"></app-icon>
            Refresh
          </button>
          <button hlmBtn (click)="createBackup()">
            <app-icon name="save" size="16"></app-icon>
            Backup
          </button>
        </div>
      </div>

      @if (dashboard(); as data) {
        <!-- Stat Cards -->
        <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <div hlmCard class="relative overflow-hidden p-5 transition-shadow hover:shadow-sm">
            <div class="flex items-center justify-between">
              <p class="text-muted-foreground text-sm font-medium">Total Devices</p>
              <div class="rounded-lg bg-primary/10 p-2 text-primary"><app-icon name="monitor" size="18"></app-icon></div>
            </div>
            <p class="mt-3 text-3xl font-bold">{{ data.summary.totalDevices }}</p>
            <p class="text-muted-foreground mt-1 text-xs">Registered in controller</p>
            <div hlmProgress class="absolute bottom-0 start-0 h-1 w-full" [value]="100"></div>
          </div>

          <div hlmCard class="relative overflow-hidden p-5 transition-shadow hover:shadow-sm">
            <div class="flex items-center justify-between">
              <p class="text-muted-foreground text-sm font-medium">Online Now</p>
              <div class="rounded-lg bg-success/10 p-2 text-success"><app-icon name="circle-check" size="18"></app-icon></div>
            </div>
            <p class="mt-3 text-3xl font-bold">{{ data.summary.onlineDevices }}</p>
            <p class="text-muted-foreground mt-1 text-xs">
              @if (data.summary.totalDevices > 0) {
                {{ (data.summary.onlineDevices / data.summary.totalDevices * 100).toFixed(0) }}% online
              }
            </p>
            <div hlmProgress class="absolute bottom-0 start-0 h-1 w-full" [value]="data.summary.totalDevices > 0 ? (data.summary.onlineDevices / data.summary.totalDevices * 100) : 0"></div>
          </div>

          <div hlmCard class="relative overflow-hidden p-5 transition-shadow hover:shadow-sm">
            <div class="flex items-center justify-between">
              <p class="text-muted-foreground text-sm font-medium">Active Targets</p>
              <div class="rounded-lg bg-warning/10 p-2 text-warning"><app-icon name="target" size="18"></app-icon></div>
            </div>
            <p class="mt-3 text-3xl font-bold">{{ data.summary.activeDevices }}</p>
            <p class="text-muted-foreground mt-1 text-xs">Automation enabled</p>
            <div hlmProgress class="absolute bottom-0 start-0 h-1 w-full" [value]="data.summary.totalDevices > 0 ? (data.summary.activeDevices / data.summary.totalDevices * 100) : 0"></div>
          </div>

          <div hlmCard class="relative overflow-hidden p-5 transition-shadow hover:shadow-sm">
            <div class="flex items-center justify-between">
              <p class="text-muted-foreground text-sm font-medium">Last Action</p>
              <div class="rounded-lg bg-primary/10 p-2 text-primary"><app-icon name="activity" size="18"></app-icon></div>
            </div>
            <p class="mt-3 line-clamp-2 text-base font-semibold">{{ data.summary.lastAction || 'No recent actions' }}</p>
            <p class="text-muted-foreground mt-1 text-xs">Most recent event</p>
          </div>
        </div>

        <div class="grid gap-6 xl:grid-cols-[1.8fr_1fr]">
          <!-- Device Table -->
          <div hlmCard>
            <div hlmCardHeader class="flex-row items-center justify-between">
              <div>
                <h3 hlmCardTitle>Managed Devices</h3>
                <p hlmCardDescription>Overview of all devices and their current status.</p>
              </div>
              <a hlmBtn variant="link" size="sm" routerLink="/devices">View all</a>
            </div>
            <div hlmCardContent class="p-0">
              <div hlmTableContainer>
                <table hlmTable>
                  <thead hlmTHead>
                    <tr hlmTr>
                      <th hlmTh>Name</th>
                      <th hlmTh>IP</th>
                      <th hlmTh>Status</th>
                      <th hlmTh>OS</th>
                      <th hlmTh>Updated</th>
                    </tr>
                  </thead>
                  <tbody hlmTBody>
                    @for (device of data.devices; track device.deviceId) {
                      <tr hlmTr>
                        <td hlmTd>
                          <p class="font-medium">{{ device.name }}</p>
                          <p class="text-muted-foreground text-xs">{{ device.lastOperationSummary || '—' }}</p>
                        </td>
                        <td hlmTd class="font-mono text-sm">{{ device.ipAddress }}</td>
                        <td hlmTd>
                          <span hlmBadge
                            [class.bg-success/10]="device.lastKnownStatus === 'Online'"
                            [class.text-success]="device.lastKnownStatus === 'Online'"
                            [class.bg-destructive/10]="device.lastKnownStatus === 'Offline'"
                            [class.text-destructive]="device.lastKnownStatus === 'Offline'"
                            [class.bg-warning/10]="device.lastKnownStatus === 'Unknown'"
                            [class.text-warning]="device.lastKnownStatus === 'Unknown'"
                            [class.bg-muted]="!['Online','Offline','Unknown'].includes(device.lastKnownStatus)"
                            class="gap-1"
                          >
                            <span class="h-1.5 w-1.5 rounded-full"
                              [class.bg-success]="device.lastKnownStatus === 'Online'"
                              [class.bg-destructive]="device.lastKnownStatus === 'Offline'"
                              [class.bg-warning]="device.lastKnownStatus === 'Unknown'"
                            ></span>
                            {{ device.lastKnownStatus }}
                          </span>
                        </td>
                        <td hlmTd class="text-sm">
                          <span class="inline-flex items-center gap-1 text-xs text-muted-foreground">
                            <app-icon [name]="device.operatingSystemType === 'Windows' ? 'monitor' : 'cpu'" size="12" />
                            {{ device.operatingSystemType }}
                          </span>
                        </td>
                        <td hlmTd class="text-sm text-muted-foreground whitespace-nowrap">{{ device.lastUpdatedDateUtc | date: 'MMM d, HH:mm' }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <!-- Recent Activity -->
          <div hlmCard>
            <div hlmCardHeader>
              <h3 hlmCardTitle>Recent Activity</h3>
              <p hlmCardDescription>Latest events from the controller.</p>
            </div>
            <div hlmCardContent class="max-h-[500px] overflow-y-auto p-0">
              @for (line of recentLogLines(); track line; let i = $index) {
                <div class="flex gap-3 border-b px-6 py-3 text-sm last:border-0">
                  <div class="mt-1.5 flex shrink-0 flex-col items-center">
                    <span class="h-2 w-2 rounded-full"
                      [class.bg-primary]="i < 3"
                      [class.bg-muted]="i >= 3"
                    ></span>
                    @if (!$last) { <span class="mt-1 h-full w-px bg-border"></span> }
                  </div>
                  <p class="text-muted-foreground leading-relaxed">{{ line }}</p>
                </div>
              } @empty {
                <div class="flex flex-col items-center gap-2 px-6 py-12 text-sm text-muted-foreground">
                  <app-icon name="logs" size="24"></app-icon>
                  <p>No recent activity</p>
                </div>
              }
            </div>
          </div>
        </div>
      } @else {
        <!-- Loading State -->
        <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          @for (_ of [1,2,3,4]; track $index) {
            <div hlmSkeleton class="h-32 w-full"></div>
          }
        </div>
        <div class="grid gap-6 xl:grid-cols-[1.8fr_1fr]">
          <div hlmSkeleton class="h-80 w-full"></div>
          <div hlmSkeleton class="h-80 w-full"></div>
        </div>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardPageComponent {
  private readonly api = inject(ControlApiService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly dashboard = signal<DashboardResponse | null>(null);
  readonly isBusy = signal(false);
  readonly autoRefresh = signal(false);
  readonly recentLogLines = computed(() => this.dashboard()?.recentLogs.slice(0, 8) ?? []);

  constructor() {
    this.load();
    const sub = interval(10000).subscribe(() => { if (this.autoRefresh()) this.load(); });
    this.destroyRef.onDestroy(() => sub.unsubscribe());
  }

  load(): void {
    this.isBusy.set(true);
    this.api.getDashboard(true, 20).pipe(finalize(() => this.isBusy.set(false))).subscribe({
      next: r => this.dashboard.set(r),
      error: e => this.toast.error(e.error?.detail ?? 'Failed to load dashboard')
    });
  }

  createBackup(): void {
    this.api.createBackup().subscribe({
      next: r => { this.toast.success(`Backup created: ${r.archivePath}`); },
      error: e => this.toast.error(e.error?.detail ?? 'Backup failed')
    });
  }
}
