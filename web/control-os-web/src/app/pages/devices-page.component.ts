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

type DeviceTab = 'all' | 'password' | 'nopassword';

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
    <section class="mx-auto flex max-w-[1500px] flex-col gap-6">
      <!-- Header -->
      <div class="overflow-hidden rounded-2xl border bg-card shadow-sm">
        <div class="relative p-6 sm:p-8">
          <div class="absolute inset-x-0 top-0 h-1 bg-primary"></div>

          <div class="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
            <div class="space-y-3">
              <div
                class="inline-flex items-center gap-2 rounded-full border bg-muted/40 px-3 py-1 text-xs font-medium text-muted-foreground"
              >
                <span class="h-2 w-2 rounded-full bg-primary"></span>
                Remote Power Control
              </div>

              <div>
                <h1 class="text-3xl font-semibold tracking-tight">Devices</h1>
                <p class="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">
                  Manage remote hosts, SSH access, Wake-on-LAN settings, and power automation from
                  one clean control panel.
                </p>
              </div>
            </div>

            <div class="flex flex-col gap-2 sm:flex-row">
              <button
                hlmBtn
                variant="outline"
                type="button"
                (click)="load()"
                [disabled]="loading() || isBusy()"
              >
                <app-icon name="refresh-cw" [size]="16" [class.animate-spin]="loading()"></app-icon>
                Refresh
              </button>

              <button hlmBtn type="button" (click)="resetForm()">
                <app-icon name="plus" [size]="16"></app-icon>
                Add Device
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Alert -->
      @if (message()) {
        <div
          class="flex items-start gap-3 rounded-xl border px-4 py-3 text-sm shadow-sm"
          [ngClass]="
            messageType() === 'success'
              ? 'border-emerald-500/25 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300'
              : 'border-destructive/25 bg-destructive/10 text-destructive'
          "
        >
          <app-icon
            [name]="messageType() === 'success' ? 'check-circle-2' : 'circle-alert'"
            [size]="18"
            class="mt-0.5 shrink-0"
          ></app-icon>
          <span>{{ message() }}</span>
        </div>
      }

      <!-- Stats -->
      <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <div hlmCard class="p-5">
          <div class="flex items-center justify-between gap-3">
            <div>
              <p class="text-sm text-muted-foreground">Total Devices</p>
              <p class="mt-1 text-3xl font-semibold">{{ stats().total }}</p>
            </div>
            <div class="rounded-xl bg-primary/10 p-3 text-primary">
              <app-icon name="server" [size]="22"></app-icon>
            </div>
          </div>
        </div>

        <div hlmCard class="p-5">
          <div class="flex items-center justify-between gap-3">
            <div>
              <p class="text-sm text-muted-foreground">Online</p>
              <p class="mt-1 text-3xl font-semibold text-emerald-600">{{ stats().online }}</p>
            </div>
            <div class="rounded-xl bg-emerald-500/10 p-3 text-emerald-600">
              <app-icon name="wifi" [size]="22"></app-icon>
            </div>
          </div>
        </div>

        <div hlmCard class="p-5">
          <div class="flex items-center justify-between gap-3">
            <div>
              <p class="text-sm text-muted-foreground">Offline</p>
              <p class="mt-1 text-3xl font-semibold text-destructive">{{ stats().offline }}</p>
            </div>
            <div class="rounded-xl bg-destructive/10 p-3 text-destructive">
              <app-icon name="wifi-off" [size]="22"></app-icon>
            </div>
          </div>
        </div>

        <div hlmCard class="p-5">
          <div class="flex items-center justify-between gap-3">
            <div>
              <p class="text-sm text-muted-foreground">With SSH Password</p>
              <p class="mt-1 text-3xl font-semibold">{{ stats().withPassword }}</p>
            </div>
            <div class="rounded-xl bg-amber-500/10 p-3 text-amber-600">
              <app-icon name="key-round" [size]="22"></app-icon>
            </div>
          </div>
        </div>
      </div>

      <div class="grid gap-6 xl:grid-cols-[minmax(0,1.35fr)_minmax(420px,0.65fr)]">
        <!-- Devices List -->
        <div hlmCard class="overflow-hidden">
          <div class="border-b p-4 sm:p-5">
            <div class="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
              <div>
                <h2 class="text-lg font-semibold">Device Inventory</h2>
                <p class="mt-1 text-sm text-muted-foreground">
                  Search, filter, edit, and run power operations.
                </p>
              </div>

              <div class="relative w-full lg:max-w-sm">
                <app-icon
                  name="search"
                  [size]="16"
                  class="absolute start-3 top-1/2 -translate-y-1/2 text-muted-foreground"
                ></app-icon>
                <input
                  hlmInput
                  class="ps-9"
                  placeholder="Search by name, IP, MAC..."
                  [formControl]="searchControl"
                />
              </div>
            </div>
          </div>

          <div
            hlmTabs
            class="border-b px-4 pt-2"
            [tab]="activeTab()"
            (tabActivated)="onTabChange($event)"
          >
            <div hlmTabsList variant="line">
              <button hlmTabsTrigger="all">All {{ stats().total }}</button>
              <button hlmTabsTrigger="password">Password {{ stats().withPassword }}</button>
              <button hlmTabsTrigger="nopassword">Key / Empty {{ stats().withoutPassword }}</button>
            </div>
          </div>

          @if (loading()) {
            <div class="space-y-3 p-5">
              @for (_ of skeletonRows; track $index) {
                <div class="rounded-xl border p-4">
                  <div class="flex items-center justify-between gap-4">
                    <div class="flex-1 space-y-2">
                      <div hlmSkeleton class="h-4 w-40"></div>
                      <div hlmSkeleton class="h-3 w-64 max-w-full"></div>
                    </div>
                    <div hlmSkeleton class="h-8 w-28"></div>
                  </div>
                </div>
              }
            </div>
          } @else if (filteredDevices().length > 0) {
            <!-- Desktop table -->
            <div class="hidden lg:block">
              <div hlmTableContainer>
                <table hlmTable>
                  <thead hlmTHead>
                    <tr hlmTr>
                      <th hlmTh class="w-[36%]">Device</th>
                      <th hlmTh>Status</th>
                      <th hlmTh>Access</th>
                      <th hlmTh>Automation</th>
                      <th hlmTh class="text-end">Actions</th>
                    </tr>
                  </thead>

                  <tbody hlmTBody>
                    @for (device of filteredDevices(); track device.deviceId) {
                      <tr hlmTr>
                        <td hlmTd>
                          <div class="flex items-center gap-3">
                            <div
                              class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border bg-muted/40"
                            >
                              <app-icon
                                name="monitor"
                                [size]="18"
                                class="text-muted-foreground"
                              ></app-icon>
                            </div>

                            <div class="min-w-0">
                              <button
                                hlmBtn
                                variant="link"
                                class="h-auto max-w-full p-0 text-start font-semibold"
                                type="button"
                                (click)="edit(device)"
                              >
                                <span class="truncate">{{ device.name }}</span>
                              </button>

                              <p class="mt-1 truncate font-mono text-xs text-muted-foreground">
                                {{ device.ipAddress }} · {{ device.macAddress || 'No MAC' }}
                              </p>
                            </div>
                          </div>
                        </td>

                        <td hlmTd>
                          <span
                            hlmBadge
                            class="gap-1.5"
                            [ngClass]="statusBadgeClass(device.lastKnownStatus)"
                          >
                            <span
                              class="h-1.5 w-1.5 rounded-full"
                              [ngClass]="statusDotClass(device.lastKnownStatus)"
                            ></span>
                            {{ device.lastKnownStatus || 'Unknown' }}
                          </span>
                        </td>

                        <td hlmTd>
                          <div class="space-y-1">
                            <div
                              class="inline-flex items-center gap-1.5 text-xs text-muted-foreground"
                            >
                              <app-icon
                                [name]="device.hasSshPassword ? 'key-round' : 'shield-check'"
                                [size]="13"
                              ></app-icon>
                              {{ device.hasSshPassword ? 'Password' : 'Key / Empty' }}
                            </div>

                            <p class="font-mono text-xs text-muted-foreground">
                              SSH {{ device.sshHost || device.ipAddress }}:{{
                                device.sshPort || 22
                              }}
                            </p>
                          </div>
                        </td>

                        <td hlmTd>
                          <div class="flex flex-wrap gap-1.5">
                            @if (device.autoStartEnabled) {
                              <span hlmBadge variant="outline">Auto Start</span>
                            }

                            @if (device.autoShutdownEnabled) {
                              <span hlmBadge variant="outline">Auto Off</span>
                            }

                            @if (!device.isActive) {
                              <span hlmBadge variant="destructive">Inactive</span>
                            }
                          </div>
                        </td>

                        <td hlmTd>
                          <div class="flex justify-end gap-1.5">
                            <button
                              hlmBtn
                              variant="outline"
                              size="xs"
                              type="button"
                              [disabled]="isOperationBusy(device.deviceId)"
                              (click)="operate(device, 'Start')"
                            >
                              <app-icon name="play" [size]="12"></app-icon>
                              Start
                            </button>

                            <button
                              hlmBtn
                              variant="outline"
                              size="xs"
                              type="button"
                              [disabled]="isOperationBusy(device.deviceId)"
                              (click)="operate(device, 'Reboot')"
                            >
                              <app-icon name="refresh-cw" [size]="12"></app-icon>
                              Reboot
                            </button>

                            <button
                              hlmBtn
                              variant="outline"
                              size="xs"
                              type="button"
                              class="text-destructive hover:bg-destructive/10"
                              [disabled]="isOperationBusy(device.deviceId)"
                              (click)="operate(device, 'Shutdown')"
                            >
                              <app-icon name="power-off" [size]="12"></app-icon>
                              Off
                            </button>

                            <button
                              hlmBtn
                              variant="outline"
                              size="icon"
                              type="button"
                              [disabled]="isBusy()"
                              (click)="requestDelete(device)"
                              aria-label="Delete device"
                            >
                              <app-icon name="trash-2" [size]="13"></app-icon>
                            </button>
                          </div>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>

            <!-- Mobile cards -->
            <div class="grid gap-3 p-4 lg:hidden">
              @for (device of filteredDevices(); track device.deviceId) {
                <article class="rounded-xl border bg-card p-4 shadow-sm">
                  <div class="flex items-start justify-between gap-3">
                    <div class="min-w-0">
                      <button
                        hlmBtn
                        variant="link"
                        class="h-auto p-0 text-start text-base font-semibold"
                        type="button"
                        (click)="edit(device)"
                      >
                        {{ device.name }}
                      </button>

                      <p class="mt-1 font-mono text-xs text-muted-foreground">
                        {{ device.ipAddress }}
                      </p>

                      <p class="mt-0.5 font-mono text-xs text-muted-foreground">
                        {{ device.macAddress || 'No MAC' }}
                      </p>
                    </div>

                    <span
                      hlmBadge
                      class="shrink-0 gap-1.5"
                      [ngClass]="statusBadgeClass(device.lastKnownStatus)"
                    >
                      <span
                        class="h-1.5 w-1.5 rounded-full"
                        [ngClass]="statusDotClass(device.lastKnownStatus)"
                      ></span>
                      {{ device.lastKnownStatus || 'Unknown' }}
                    </span>
                  </div>

                  <div class="mt-4 grid gap-2 text-xs text-muted-foreground">
                    <div class="flex items-center gap-2">
                      <app-icon name="terminal" [size]="14"></app-icon>
                      <span class="font-mono"
                        >SSH {{ device.sshHost || device.ipAddress }}:{{
                          device.sshPort || 22
                        }}</span
                      >
                    </div>

                    <div class="flex items-center gap-2">
                      <app-icon
                        [name]="device.hasSshPassword ? 'key-round' : 'shield-check'"
                        [size]="14"
                      ></app-icon>
                      <span>{{
                        device.hasSshPassword ? 'Password access' : 'Key / empty password'
                      }}</span>
                    </div>
                  </div>

                  <div class="mt-4 grid grid-cols-2 gap-2">
                    <button
                      hlmBtn
                      variant="outline"
                      size="sm"
                      type="button"
                      [disabled]="isOperationBusy(device.deviceId)"
                      (click)="operate(device, 'Start')"
                    >
                      <app-icon name="play" [size]="13"></app-icon>
                      Start
                    </button>

                    <button
                      hlmBtn
                      variant="outline"
                      size="sm"
                      type="button"
                      [disabled]="isOperationBusy(device.deviceId)"
                      (click)="operate(device, 'Reboot')"
                    >
                      <app-icon name="refresh-cw" [size]="13"></app-icon>
                      Reboot
                    </button>

                    <button
                      hlmBtn
                      variant="outline"
                      size="sm"
                      type="button"
                      class="text-destructive hover:bg-destructive/10"
                      [disabled]="isOperationBusy(device.deviceId)"
                      (click)="operate(device, 'Shutdown')"
                    >
                      <app-icon name="power-off" [size]="13"></app-icon>
                      Off
                    </button>

                    <button
                      hlmBtn
                      variant="outline"
                      size="sm"
                      type="button"
                      [disabled]="isBusy()"
                      (click)="requestDelete(device)"
                    >
                      <app-icon name="trash-2" [size]="13"></app-icon>
                      Delete
                    </button>
                  </div>
                </article>
              }
            </div>
          } @else {
            <div
              class="flex min-h-[360px] flex-col items-center justify-center gap-4 p-8 text-center"
            >
              <div
                class="flex h-16 w-16 items-center justify-center rounded-2xl border bg-muted/40"
              >
                <app-icon
                  [name]="devices().length === 0 ? 'server-off' : 'search-x'"
                  [size]="30"
                  class="text-muted-foreground"
                ></app-icon>
              </div>

              <div>
                <h3 class="text-base font-semibold">
                  {{ devices().length === 0 ? 'No devices registered yet' : 'No devices found' }}
                </h3>
                <p class="mt-1 max-w-sm text-sm text-muted-foreground">
                  {{
                    devices().length === 0
                      ? 'Add your first remote host and configure SSH or Wake-on-LAN.'
                      : 'Try changing your search text or selected tab.'
                  }}
                </p>
              </div>

              @if (devices().length === 0) {
                <button hlmBtn type="button" (click)="resetForm()">
                  <app-icon name="plus" [size]="16"></app-icon>
                  Add Device
                </button>
              }
            </div>
          }
        </div>

        <!-- Form -->
        <div hlmCard class="h-fit overflow-hidden xl:sticky xl:top-4">
          <div hlmCardHeader class="border-b">
            <div class="flex items-start justify-between gap-4">
              <div>
                <h3 hlmCardTitle>{{ selectedDeviceId() ? 'Edit Device' : 'New Device' }}</h3>
                <p hlmCardDescription>
                  {{
                    selectedDeviceId()
                      ? 'Modify host configuration and automation settings.'
                      : 'Register a host for remote control.'
                  }}
                </p>
              </div>

              <div class="rounded-xl border bg-muted/40 p-2 text-muted-foreground">
                <app-icon [name]="selectedDeviceId() ? 'pencil' : 'plus'" [size]="18"></app-icon>
              </div>
            </div>
          </div>

          <form [formGroup]="form" (ngSubmit)="save()">
            <div hlmCardContent class="space-y-6 p-5">
              <!-- Basic -->
              <section class="space-y-4">
                <div class="flex items-center gap-2">
                  <div class="h-2 w-2 rounded-full bg-primary"></div>
                  <h4 class="text-sm font-semibold">Basic Information</h4>
                </div>

                <div class="space-y-1.5">
                  <label hlmLabel for="device-name">Device Name</label>
                  <input
                    hlmInput
                    id="device-name"
                    formControlName="name"
                    placeholder="Example: Main Ubuntu Server"
                  />
                </div>

                <div class="grid gap-4 sm:grid-cols-2">
                  <div class="space-y-1.5">
                    <label hlmLabel for="device-ip">IP Address</label>
                    <input
                      hlmInput
                      id="device-ip"
                      formControlName="ipAddress"
                      class="font-mono"
                      placeholder="192.168.1.18"
                    />
                  </div>

                  <div class="space-y-1.5">
                    <label hlmLabel for="device-mac">MAC Address</label>
                    <input
                      hlmInput
                      id="device-mac"
                      formControlName="macAddress"
                      class="font-mono"
                      placeholder="AA:BB:CC:DD:EE:FF"
                    />
                  </div>
                </div>
              </section>

              <!-- SSH -->
              <section class="rounded-xl border">
                <div class="flex items-center justify-between gap-3 border-b px-4 py-3">
                  <div>
                    <h4 class="text-sm font-semibold">SSH Access</h4>
                    <p class="mt-0.5 text-xs text-muted-foreground">
                      Used for shutdown and reboot commands.
                    </p>
                  </div>
                  <app-icon name="terminal" [size]="17" class="text-muted-foreground"></app-icon>
                </div>

                <div class="space-y-4 p-4">
                  <div class="grid gap-4 sm:grid-cols-2">
                    <div class="space-y-1.5">
                      <label hlmLabel for="ssh-host">SSH Host</label>
                      <input
                        hlmInput
                        id="ssh-host"
                        formControlName="sshHost"
                        class="font-mono"
                        placeholder="Same as IP or hostname"
                      />
                    </div>

                    <div class="space-y-1.5">
                      <label hlmLabel for="ssh-port">SSH Port</label>
                      <input hlmInput id="ssh-port" type="number" formControlName="sshPort" />
                    </div>
                  </div>

                  <div class="grid gap-4 sm:grid-cols-2">
                    <div class="space-y-1.5">
                      <label hlmLabel for="ssh-user">SSH Username</label>
                      <input
                        hlmInput
                        id="ssh-user"
                        formControlName="sshUsername"
                        placeholder="ubuntu / root / user"
                      />
                    </div>

                    <div class="space-y-1.5">
                      <label hlmLabel for="ssh-password">SSH Password</label>
                      <input
                        hlmInput
                        id="ssh-password"
                        type="password"
                        formControlName="sshPassword"
                        placeholder="Leave empty for key-based access"
                      />
                    </div>
                  </div>
                </div>
              </section>

              <!-- WOL -->
              <section class="rounded-xl border">
                <div class="flex items-center justify-between gap-3 border-b px-4 py-3">
                  <div>
                    <h4 class="text-sm font-semibold">Wake-on-LAN</h4>
                    <p class="mt-0.5 text-xs text-muted-foreground">
                      Used to start devices through magic packet.
                    </p>
                  </div>
                  <app-icon name="radio" [size]="17" class="text-muted-foreground"></app-icon>
                </div>

                <div class="grid gap-4 p-4 sm:grid-cols-2">
                  <div class="space-y-1.5">
                    <label hlmLabel for="broadcast">Broadcast Address</label>
                    <input
                      hlmInput
                      id="broadcast"
                      formControlName="broadcastAddress"
                      class="font-mono"
                    />
                  </div>

                  <div class="space-y-1.5">
                    <label hlmLabel for="wol-port">WOL Port</label>
                    <input hlmInput id="wol-port" type="number" formControlName="wolPort" />
                  </div>
                </div>
              </section>

              <!-- Advanced -->
              <section class="rounded-xl border">
                <div class="flex items-center justify-between gap-3 border-b px-4 py-3">
                  <div>
                    <h4 class="text-sm font-semibold">Execution Settings</h4>
                    <p class="mt-0.5 text-xs text-muted-foreground">Timeout and retry behavior.</p>
                  </div>
                  <app-icon name="settings-2" [size]="17" class="text-muted-foreground"></app-icon>
                </div>

                <div class="grid gap-4 p-4 sm:grid-cols-2">
                  <div class="space-y-1.5">
                    <label hlmLabel for="timeout">Timeout Seconds</label>
                    <input hlmInput id="timeout" type="number" formControlName="timeoutSeconds" />
                  </div>

                  <div class="space-y-1.5">
                    <label hlmLabel for="retry">Retry Count</label>
                    <input hlmInput id="retry" type="number" formControlName="retryCount" />
                  </div>
                </div>
              </section>

              <!-- Automation -->
              <section class="rounded-xl border">
                <div class="flex items-center justify-between gap-3 border-b px-4 py-3">
                  <div>
                    <h4 class="text-sm font-semibold">Automation Flags</h4>
                    <p class="mt-0.5 text-xs text-muted-foreground">
                      Control automatic and manual actions.
                    </p>
                  </div>
                  <app-icon name="workflow" [size]="17" class="text-muted-foreground"></app-icon>
                </div>

                <div class="divide-y">
                  @for (flag of automationFlags; track flag.key) {
                    <label
                      class="flex cursor-pointer items-center justify-between gap-4 px-4 py-3 text-sm"
                    >
                      <span>
                        <span class="font-medium">{{ flag.label }}</span>
                        <span class="mt-0.5 block text-xs text-muted-foreground">{{
                          flag.description
                        }}</span>
                      </span>

                      <hlm-switch [formControlName]="flag.key" />
                    </label>
                  }
                </div>
              </section>

              <div class="space-y-1.5">
                <label hlmLabel for="description">Description</label>
                <textarea
                  hlmTextarea
                  id="description"
                  formControlName="description"
                  rows="4"
                  placeholder="Optional notes about this device..."
                ></textarea>
              </div>
            </div>

            <div hlmCardFooter class="flex flex-col gap-2 border-t p-5 sm:flex-row">
              <button hlmBtn type="submit" class="w-full sm:flex-1" [disabled]="isBusy()">
                @if (isBusy()) {
                  <app-icon name="loader-2" [size]="16" class="animate-spin"></app-icon>
                  Saving...
                } @else {
                  <app-icon [name]="selectedDeviceId() ? 'save' : 'plus'" [size]="16"></app-icon>
                  {{ selectedDeviceId() ? 'Save Changes' : 'Create Device' }}
                }
              </button>

              @if (selectedDeviceId()) {
                <button
                  hlmBtn
                  variant="outline"
                  type="button"
                  class="w-full sm:w-auto"
                  [disabled]="isBusy()"
                  (click)="resetForm()"
                >
                  Cancel
                </button>
              }
            </div>
          </form>
        </div>
      </div>
    </section>

    <!-- Delete Modal -->
    @if (deleteTarget(); as target) {
      <div
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm"
      >
        <div
          class="w-full max-w-md rounded-2xl border bg-popover p-6 text-popover-foreground shadow-2xl"
        >
          <div class="flex items-start gap-4">
            <div class="rounded-xl bg-destructive/10 p-3 text-destructive">
              <app-icon name="trash-2" [size]="22"></app-icon>
            </div>

            <div class="min-w-0 flex-1">
              <h3 class="text-base font-semibold">Delete Device</h3>
              <p class="mt-2 text-sm leading-6 text-muted-foreground">
                Are you sure you want to delete
                <span class="font-semibold text-foreground">"{{ target.name }}"</span>? This action
                cannot be undone.
              </p>
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-2">
            <button hlmBtn variant="outline" type="button" (click)="deleteTarget.set(null)">
              Cancel
            </button>

            <button hlmBtn variant="destructive" type="button" (click)="confirmDelete(target)">
              Delete
            </button>
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
  readonly activeTab = signal<DeviceTab>('all');

  readonly skeletonRows = [1, 2, 3, 4, 5];

  readonly automationFlags = [
    {
      key: 'autoStartEnabled' as const,
      label: 'Auto-start on controller boot',
      description: 'Start this device automatically when the controller app starts.',
    },
    {
      key: 'autoShutdownEnabled' as const,
      label: 'Auto-shutdown on controller stop',
      description: 'Shutdown this device when the controller app stops.',
    },
    {
      key: 'manualControlEnabled' as const,
      label: 'Manual control via UI',
      description: 'Allow Start, Reboot, and Shutdown actions from this page.',
    },
    {
      key: 'isActive' as const,
      label: 'Active target',
      description: 'Keep this device enabled in automation and lists.',
    },
  ];

  readonly stats = computed(() => {
    const devices = this.devices();

    return {
      total: devices.length,
      online: devices.filter((device) => device.lastKnownStatus === 'Online').length,
      offline: devices.filter((device) => device.lastKnownStatus === 'Offline').length,
      unknown: devices.filter(
        (device) => !device.lastKnownStatus || device.lastKnownStatus === 'Unknown',
      ).length,
      withPassword: devices.filter((device) => device.hasSshPassword).length,
      withoutPassword: devices.filter((device) => !device.hasSshPassword).length,
    };
  });

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

        return (
          name.includes(query) ||
          ip.includes(query) ||
          mac.includes(query) ||
          sshHost.includes(query)
        );
      });
    }

    if (tab === 'password') {
      return list.filter((device) => device.hasSshPassword);
    }

    if (tab === 'nopassword') {
      return list.filter((device) => !device.hasSshPassword);
    }

    return list;
  });

  readonly form = this.formBuilder.group({
    name: '',
    ipAddress: '',
    macAddress: '',
    broadcastAddress: '255.255.255.255',
    wolPort: 9,
    sshHost: '',
    sshPort: 22,
    sshUsername: '',
    sshPassword: '',
    operatingSystemType: this.formBuilder.control<DeviceOperatingSystemType>('Linux'),
    autoStartEnabled: true,
    autoShutdownEnabled: true,
    manualControlEnabled: true,
    timeoutSeconds: 15,
    retryCount: 1,
    description: '',
    isActive: true,
  });

  constructor() {
    this.load();

    this.searchControl.valueChanges.subscribe((value) => {
      this.searchQuery.set(value ?? '');
    });
  }

  onTabChange(value: string): void {
    if (value === 'all' || value === 'password' || value === 'nopassword') {
      this.activeTab.set(value);
    }
  }

  load(): void {
    this.loading.set(true);

    this.api.getDevices().subscribe({
      next: (devices) => {
        this.devices.set(devices);
        this.loading.set(false);
      },
      error: (error) => {
        this.toast.error(error.error?.detail ?? 'Failed to load devices');
        this.loading.set(false);
      },
    });
  }

  edit(device: Device): void {
    this.selectedDeviceId.set(device.deviceId);

    this.form.patchValue({
      ...device,
      sshPassword: '',
    } as unknown as Partial<typeof this.form.value>);
  }

  resetForm(): void {
    this.selectedDeviceId.set(null);

    this.form.reset({
      name: '',
      ipAddress: '',
      macAddress: '',
      broadcastAddress: '255.255.255.255',
      wolPort: 9,
      sshHost: '',
      sshPort: 22,
      sshUsername: '',
      sshPassword: '',
      operatingSystemType: 'Linux',
      autoStartEnabled: true,
      autoShutdownEnabled: true,
      manualControlEnabled: true,
      timeoutSeconds: 15,
      retryCount: 1,
      description: '',
      isActive: true,
    });
  }

  save(): void {
    const payload = this.form.getRawValue() as DeviceUpsertRequest;

    const request$ = this.selectedDeviceId()
      ? this.api.updateDevice(this.selectedDeviceId()!, payload)
      : this.api.createDevice(payload);

    this.isBusy.set(true);

    request$.pipe(finalize(() => this.isBusy.set(false))).subscribe({
      next: () => {
        this.messageType.set('success');
        this.message.set(this.selectedDeviceId() ? 'Device updated.' : 'Device created.');

        this.toast.success(this.message());
        this.resetForm();
        this.load();
      },
      error: (error) => {
        this.messageType.set('error');
        this.message.set(error.error?.detail ?? 'Failed to save device.');

        this.toast.error(this.message());
      },
    });
  }

  requestDelete(device: Device): void {
    this.deleteTarget.set(device);
  }

  confirmDelete(device: Device): void {
    this.deleteTarget.set(null);
    this.isBusy.set(true);

    this.api
      .deleteDevice(device.deviceId)
      .pipe(finalize(() => this.isBusy.set(false)))
      .subscribe({
        next: () => {
          this.toast.success(`Removed ${device.name}.`);
          this.load();
        },
        error: (error) => {
          this.toast.error(error.error?.detail ?? 'Failed to delete device.');
        },
      });
  }

  operate(device: Device, operation: DevicePowerOperation): void {
    this.operationBusyDeviceId.set(device.deviceId);

    this.api
      .executeOperation(device.deviceId, operation)
      .pipe(finalize(() => this.operationBusyDeviceId.set(null)))
      .subscribe({
        next: (response) => {
          this.toast.success(response.message);
          this.load();
        },
        error: (error) => {
          this.toast.error(error.error?.detail ?? `Failed to execute ${operation}.`);
        },
      });
  }

  isOperationBusy(deviceId: string): boolean {
    return this.operationBusyDeviceId() === deviceId;
  }

  statusBadgeClass(status: string | null | undefined): string {
    if (status === 'Online') {
      return 'border-emerald-500/25 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300';
    }

    if (status === 'Offline') {
      return 'border-destructive/25 bg-destructive/10 text-destructive';
    }

    return 'border-amber-500/25 bg-amber-500/10 text-amber-700 dark:text-amber-300';
  }

  statusDotClass(status: string | null | undefined): string {
    if (status === 'Online') {
      return 'bg-emerald-500';
    }

    if (status === 'Offline') {
      return 'bg-destructive';
    }

    return 'bg-amber-500';
  }
}
