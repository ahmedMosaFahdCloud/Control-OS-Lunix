import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { ControlApiService } from '../core/control-api.service';
import {
  Device,
  DeviceOperatingSystemType,
  DevicePowerOperation,
  DeviceUpsertRequest,
} from '../core/api.models';
import { DEFAULTS, API, STATUS_BADGE, TAB, STATUS, ERROR_MSGS, OPERATION } from '../core/constants';
import { IconComponent } from '../shared/components/icon.component';
import { ToastService } from '../shared/services/toast.service';

import { HlmButton } from '@spartan-ng/helm/button';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmLabel } from '@spartan-ng/helm/label';
import { HlmTextarea } from '@spartan-ng/helm/textarea';
import { HlmSwitch } from '@spartan-ng/helm/switch';
import {
  HlmCard,
  HlmCardHeader,
  HlmCardTitle,
  HlmCardDescription,
  HlmCardContent,
  HlmCardFooter,
} from '@spartan-ng/helm/card';
import {
  HlmTableContainer,
  HlmTable,
  HlmTHead,
  HlmTBody,
  HlmTr,
  HlmTh,
  HlmTd,
} from '@spartan-ng/helm/table';
import { HlmSkeleton } from '@spartan-ng/helm/skeleton';
import { HlmTabsImports } from '@spartan-ng/helm/tabs';

type DeviceTab = typeof TAB.ALL | typeof TAB.PASSWORD | typeof TAB.NO_PASSWORD;

const FORM_DEFAULTS = {
  name: DEFAULTS.NAME,
  ipAddress: DEFAULTS.IP_ADDRESS,
  macAddress: DEFAULTS.MAC_ADDRESS,
  broadcastAddress: DEFAULTS.BROADCAST_ADDRESS,
  wolPort: DEFAULTS.WOL_PORT,
  sshHost: DEFAULTS.SSH_HOST,
  sshPort: DEFAULTS.SSH_PORT,
  sshUsername: DEFAULTS.SSH_USERNAME,
  sshPassword: DEFAULTS.SSH_PASSWORD,
  operatingSystemType: DEFAULTS.OS_TYPE as DeviceOperatingSystemType,
  autoStartEnabled: DEFAULTS.AUTO_START,
  autoShutdownEnabled: DEFAULTS.AUTO_SHUTDOWN,
  manualControlEnabled: DEFAULTS.MANUAL_CONTROL,
  timeoutSeconds: DEFAULTS.TIMEOUT_SECONDS,
  retryCount: DEFAULTS.RETRY_COUNT,
  description: DEFAULTS.DESCRIPTION,
  isActive: DEFAULTS.IS_ACTIVE,
} as const;

@Component({
  selector: 'app-devices-page',
  standalone: true,
  imports: [
    NgClass,
    ReactiveFormsModule,
    IconComponent,

    HlmButton,
    HlmBadge,
    HlmInput,
    HlmLabel,
    HlmTextarea,
    HlmSwitch,
    HlmSkeleton,

    HlmCard,
    HlmCardHeader,
    HlmCardTitle,
    HlmCardDescription,
    HlmCardContent,
    HlmCardFooter,

    HlmTableContainer,
    HlmTable,
    HlmTHead,
    HlmTBody,
    HlmTr,
    HlmTh,
    HlmTd,

    ...HlmTabsImports,
  ],
  template: `
    <section class="space-y-4">
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 class="text-2xl font-semibold tracking-tight">Devices</h1>
          <p class="text-sm text-muted-foreground">Manage devices and power actions.</p>
        </div>
        <div class="flex gap-2">
          <button hlmBtn variant="outline" type="button" [disabled]="loading() || isBusy()" (click)="load()">
            <app-icon name="refresh-cw" [size]="16" [class.animate-spin]="loading()"></app-icon>
            Refresh
          </button>
        </div>
      </div>

      @if (message()) {
        <div
          class="rounded-lg border px-4 py-3 text-sm"
          [ngClass]="
            messageType() === 'success'
              ? 'border-emerald-500/25 bg-emerald-500/10 text-emerald-700'
              : 'border-destructive/25 bg-destructive/10 text-destructive'
          "
        >
          {{ message() }}
        </div>
      }

      <div class="grid gap-4 xl:grid-cols-[1fr_420px]">
        <div hlmCard class="overflow-hidden">
          <div class="flex flex-col gap-3 border-b p-4 lg:flex-row lg:items-center lg:justify-between">
            <div class="relative w-full lg:max-w-sm">
              <app-icon name="search" [size]="16" class="absolute start-3 top-1/2 -translate-y-1/2 text-muted-foreground"></app-icon>
              <input hlmInput class="ps-9" placeholder="Search name, IP, MAC..." [formControl]="searchControl" />
            </div>
            <div class="text-sm text-muted-foreground">{{ filteredDevices().length }} / {{ devices().length }} devices</div>
          </div>

          <div hlmTabs class="border-b px-4 pt-2" [tab]="activeTab()" (tabActivated)="onTabChange($event)">
            <div hlmTabsList variant="line">
              <button hlmTabsTrigger="all">All</button>
              <button hlmTabsTrigger="password">With Password</button>
              <button hlmTabsTrigger="nopassword">No Password</button>
            </div>
          </div>

          @if (loading()) {
            <div class="space-y-2 p-4">
              @for (_ of API.SKELETON_FOUR; track $index) {
                <div hlmSkeleton class="h-12 w-full"></div>
              }
            </div>
          } @else if (filteredDevices().length > 0) {
            <div hlmTableContainer>
              <table hlmTable>
                <thead hlmTHead>
                  <tr hlmTr>
                    <th hlmTh>Device</th>
                    <th hlmTh>Status</th>
                    <th hlmTh>SSH</th>
                    <th hlmTh class="text-end">Actions</th>
                  </tr>
                </thead>
                <tbody hlmTBody>
                  @for (device of filteredDevices(); track device.deviceId) {
                    <tr hlmTr>
                      <td hlmTd>
                        <button hlmBtn variant="link" type="button" class="h-auto p-0 font-medium" (click)="edit(device)">
                          {{ device.name }}
                        </button>
                        <p class="mt-1 font-mono text-xs text-muted-foreground">{{ device.ipAddress }} · {{ device.macAddress || 'No MAC' }}</p>
                      </td>
                      <td hlmTd>
                        <span hlmBadge [ngClass]="statusBadgeClass(device.lastKnownStatus)">
                          {{ device.lastKnownStatus || STATUS.UNKNOWN }}
                        </span>
                      </td>
                      <td hlmTd>
                        <span class="text-xs text-muted-foreground">{{ device.hasSshPassword ? 'Password' : 'Key / Empty' }}</span>
                      </td>
                      <td hlmTd>
                        <div class="flex justify-end gap-1">
                          <button hlmBtn variant="outline" size="xs" type="button"
                            [disabled]="isOperationBusy(device.deviceId)"
                            (click)="operate(device, OPERATION.START)">
                            <app-icon name="play" [size]="12"></app-icon> Start
                          </button>
                          <button hlmBtn variant="outline" size="xs" type="button"
                            [disabled]="isOperationBusy(device.deviceId)"
                            (click)="operate(device, OPERATION.REBOOT)">
                            <app-icon name="refresh-cw" [size]="12"></app-icon> Reboot
                          </button>
                          <button hlmBtn variant="outline" size="xs" type="button" class="text-destructive"
                            [disabled]="isOperationBusy(device.deviceId)"
                            (click)="operate(device, OPERATION.SHUTDOWN)">
                            <app-icon name="power-off" [size]="12"></app-icon> Off
                          </button>
                          <button hlmBtn variant="ghost" size="icon" type="button"
                            [disabled]="isBusy()"
                            (click)="requestDelete(device)">
                            <app-icon name="trash-2" [size]="14"></app-icon>
                          </button>
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          } @else {
            <div class="flex min-h-52 flex-col items-center justify-center gap-2 p-6 text-center">
              <app-icon name="server-off" [size]="28" class="text-muted-foreground"></app-icon>
              <p class="text-sm font-medium">No devices found</p>
              <p class="text-xs text-muted-foreground">Add a device or change your search filter.</p>
            </div>
          }
        </div>

        <div hlmCard>
          <div hlmCardHeader>
            <h3 hlmCardTitle>{{ selectedDeviceId() ? 'Edit Device' : 'New Device' }}</h3>
            <p hlmCardDescription>{{ selectedDeviceId() ? 'Update device data.' : 'Add new remote device.' }}</p>
          </div>

          <form [formGroup]="form" (ngSubmit)="save()">
            <div hlmCardContent class="space-y-4">
              <div class="space-y-1.5">
                <label hlmLabel for="name">Name</label>
                <input hlmInput id="name" formControlName="name" />
              </div>

              <div class="grid gap-3 sm:grid-cols-2">
                <div class="space-y-1.5">
                  <label hlmLabel for="ipAddress">IP Address</label>
                  <input hlmInput id="ipAddress" formControlName="ipAddress" class="font-mono" />
                </div>
                <div class="space-y-1.5">
                  <label hlmLabel for="macAddress">MAC Address</label>
                  <input hlmInput id="macAddress" formControlName="macAddress" class="font-mono" />
                </div>
              </div>

              <details class="rounded-lg border" open>
                <summary class="cursor-pointer px-3 py-2 text-sm font-medium">SSH</summary>
                <div class="space-y-3 border-t p-3">
                  <div class="grid gap-3 sm:grid-cols-2">
                    <div class="space-y-1.5">
                      <label hlmLabel for="sshHost">Host</label>
                      <input hlmInput id="sshHost" formControlName="sshHost" class="font-mono" />
                    </div>
                    <div class="space-y-1.5">
                      <label hlmLabel for="sshPort">Port</label>
                      <input hlmInput id="sshPort" type="number" formControlName="sshPort" />
                    </div>
                  </div>
                  <div class="grid gap-3 sm:grid-cols-2">
                    <div class="space-y-1.5">
                      <label hlmLabel for="sshUsername">Username</label>
                      <input hlmInput id="sshUsername" formControlName="sshUsername" />
                    </div>
                    <div class="space-y-1.5">
                      <label hlmLabel for="sshPassword">Password</label>
                      <input hlmInput id="sshPassword" type="password" formControlName="sshPassword" />
                    </div>
                  </div>
                </div>
              </details>

              <details class="rounded-lg border">
                <summary class="cursor-pointer px-3 py-2 text-sm font-medium">Wake-on-LAN</summary>
                <div class="grid gap-3 border-t p-3 sm:grid-cols-2">
                  <div class="space-y-1.5">
                    <label hlmLabel for="broadcastAddress">Broadcast</label>
                    <input hlmInput id="broadcastAddress" formControlName="broadcastAddress" class="font-mono" />
                  </div>
                  <div class="space-y-1.5">
                    <label hlmLabel for="wolPort">WOL Port</label>
                    <input hlmInput id="wolPort" type="number" formControlName="wolPort" />
                  </div>
                </div>
              </details>

              <details class="rounded-lg border">
                <summary class="cursor-pointer px-3 py-2 text-sm font-medium">Automation</summary>
                <div class="space-y-3 border-t p-3">
                  @for (flag of automationFlags; track flag.key) {
                    <label class="flex items-center justify-between gap-3 text-sm">
                      <span>{{ flag.label }}</span>
                      <hlm-switch [formControlName]="flag.key" />
                    </label>
                  }
                </div>
              </details>

              <div class="space-y-1.5">
                <label hlmLabel for="description">Description</label>
                <textarea hlmTextarea id="description" formControlName="description" rows="3"></textarea>
              </div>
            </div>

            <div hlmCardFooter class="gap-2">
              <button hlmBtn type="submit" class="flex-1" [disabled]="isBusy()">
                @if (isBusy()) {
                  <app-icon name="loader-2" [size]="16" class="animate-spin"></app-icon>
                  Saving
                } @else {
                  {{ selectedDeviceId() ? 'Save Changes' : 'Create Device' }}
                }
              </button>

              @if (selectedDeviceId()) {
                <button hlmBtn variant="outline" type="button" [disabled]="isBusy()" (click)="resetForm()">
                  Cancel
                </button>
              }
            </div>
          </form>
        </div>
      </div>
    </section>

    @if (deleteTarget(); as target) {
      <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
        <div class="w-full max-w-sm rounded-lg border bg-popover p-5 shadow-lg">
          <h3 class="font-semibold">Delete Device</h3>
          <p class="mt-2 text-sm text-muted-foreground">Delete "{{ target.name }}"?"</p>
          <div class="mt-5 flex justify-end gap-2">
            <button hlmBtn variant="outline" type="button" (click)="deleteTarget.set(null)">Cancel</button>
            <button hlmBtn variant="destructive" type="button" (click)="confirmDelete(target)">Delete</button>
          </div>
        </div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DevicesPageComponent {
  private readonly api = inject(ControlApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly toast = inject(ToastService);

  protected readonly API = API;
  protected readonly STATUS = STATUS;
  protected readonly OPERATION = OPERATION;

  readonly devices = signal<Device[]>([]);
  readonly selectedDeviceId = signal<string | null>(null);
  readonly deleteTarget = signal<Device | null>(null);

  readonly message = signal('');
  readonly messageType = signal<'success' | 'error'>('success');

  readonly loading = signal(true);
  readonly isBusy = signal(false);
  readonly operationBusyDeviceId = signal<string | null>(null);

  readonly searchControl = this.formBuilder.control('');
  readonly searchQuery = signal('');
  readonly activeTab = signal<DeviceTab>(TAB.ALL);

  readonly automationFlags = [
    { key: 'autoStartEnabled' as const, label: 'Auto start' },
    { key: 'autoShutdownEnabled' as const, label: 'Auto shutdown' },
    { key: 'manualControlEnabled' as const, label: 'Manual control' },
    { key: 'isActive' as const, label: 'Active' },
  ];

  readonly filteredDevices = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    const tab = this.activeTab();

    let list = this.devices();

    if (query) {
      list = list.filter((device) => {
        const name = device.name?.toLowerCase() ?? '';
        const ip = device.ipAddress?.toLowerCase() ?? '';
        const mac = device.macAddress?.toLowerCase() ?? '';
        const sshHost = device.sshHost?.toLowerCase() ?? '';

        return name.includes(query) || ip.includes(query) || mac.includes(query) || sshHost.includes(query);
      });
    }

    if (tab === TAB.PASSWORD) return list.filter((device) => device.hasSshPassword);
    if (tab === TAB.NO_PASSWORD) return list.filter((device) => !device.hasSshPassword);
    return list;
  });

  readonly form = this.formBuilder.group(FORM_DEFAULTS);

  constructor() {
    this.load();
    this.searchControl.valueChanges.subscribe((value) => this.searchQuery.set(value ?? ''));
  }

  onTabChange(value: string): void {
    if (isValidTab(value)) this.activeTab.set(value);
  }

  load(): void {
    this.loading.set(true);
    this.api.getDevices().subscribe({
      next: (devices) => { this.devices.set(devices); this.loading.set(false); },
      error: () => { this.toast.error(ERROR_MSGS.LOAD_DEVICES); this.loading.set(false); },
    });
  }

  edit(device: Device): void {
    this.selectedDeviceId.set(device.deviceId);
    this.form.patchValue({ ...device, sshPassword: '' } as unknown as Partial<typeof this.form.value>);
  }

  resetForm(): void {
    this.selectedDeviceId.set(null);
    this.form.reset(FORM_DEFAULTS);
  }

  save(): void {
    const payload = this.form.getRawValue() as DeviceUpsertRequest;
    const request$ = this.selectedDeviceId()
      ? this.api.updateDevice(this.selectedDeviceId()!, payload)
      : this.api.createDevice(payload);

    this.isBusy.set(true);
    request$.pipe(finalize(() => this.isBusy.set(false))).subscribe({
      next: () => {
        const msg = this.selectedDeviceId() ? 'Device updated.' : 'Device created.';
        this.messageType.set('success');
        this.message.set(msg);
        this.toast.success(msg);
        this.resetForm();
        this.load();
      },
      error: (error) => {
        this.messageType.set('error');
        this.message.set(error.error?.detail ?? ERROR_MSGS.SAVE_DEVICE);
        this.toast.error(this.message());
      },
    });
  }

  requestDelete(device: Device): void { this.deleteTarget.set(device); }

  confirmDelete(device: Device): void {
    this.deleteTarget.set(null);
    this.isBusy.set(true);
    this.api.deleteDevice(device.deviceId).pipe(finalize(() => this.isBusy.set(false))).subscribe({
      next: () => { this.toast.success(`Removed ${device.name}.`); this.load(); },
      error: (error) => this.toast.error(error.error?.detail ?? ERROR_MSGS.DELETE_DEVICE),
    });
  }

  operate(device: Device, operation: DevicePowerOperation): void {
    this.operationBusyDeviceId.set(device.deviceId);
    this.api.executeOperation(device.deviceId, operation).pipe(finalize(() => this.operationBusyDeviceId.set(null))).subscribe({
      next: (response) => { this.toast.success(response.message); this.load(); },
      error: (error) => this.toast.error(error.error?.detail ?? `Failed to execute ${operation}.`),
    });
  }

  isOperationBusy(deviceId: string): boolean { return this.operationBusyDeviceId() === deviceId; }

  statusBadgeClass(status: string | null | undefined): string {
    return STATUS_BADGE[status ?? ''] ?? 'bg-muted text-muted-foreground';
  }
}

function isValidTab(value: string): value is DeviceTab {
  return value === TAB.ALL || value === TAB.PASSWORD || value === TAB.NO_PASSWORD;
}
