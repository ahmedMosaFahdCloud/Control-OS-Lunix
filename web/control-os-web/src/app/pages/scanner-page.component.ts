import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { ControlApiService } from '../core/control-api.service';
import { DeviceUpsertRequest, NetworkScanResult } from '../core/api.models';
import { IconComponent } from '../shared/components/icon.component';
import { ToastService } from '../shared/services/toast.service';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmLabel } from '@spartan-ng/helm/label';
import { HlmCard, HlmCardHeader, HlmCardTitle, HlmCardDescription, HlmCardContent, HlmCardFooter } from '@spartan-ng/helm/card';

@Component({
  selector: 'app-scanner-page',
  standalone: true,
  imports: [ReactiveFormsModule, IconComponent,
    HlmButton, HlmInput, HlmLabel,
    HlmCard, HlmCardHeader, HlmCardTitle, HlmCardDescription, HlmCardContent, HlmCardFooter],
  template: `
    <div class="space-y-6">
      <div>
        <h1 class="text-2xl font-semibold tracking-tight">Network Scanner</h1>
        <p class="text-muted-foreground mt-1 text-sm">Discover reachable hosts and add them as managed devices.</p>
      </div>

      <div class="grid gap-6 xl:grid-cols-[1fr_1.2fr]">
        <div hlmCard>
          <div hlmCardHeader>
            <h3 hlmCardTitle>Scan Configuration</h3>
            <p hlmCardDescription>Define the subnet range to scan.</p>
          </div>
          <form [formGroup]="scanForm" (ngSubmit)="scan()">
            <div hlmCardContent class="space-y-4">
              <div class="space-y-1.5">
                <label hlmLabel for="subnet-prefix">Subnet Prefix</label>
                <input hlmInput id="subnet-prefix" formControlName="subnetPrefix" class="font-mono" />
              </div>
              <div class="grid gap-4 sm:grid-cols-2">
                <div class="space-y-1.5">
                  <label hlmLabel for="start-host">Start Host</label>
                  <input hlmInput id="start-host" type="number" formControlName="startHost" />
                </div>
                <div class="space-y-1.5">
                  <label hlmLabel for="end-host">End Host</label>
                  <input hlmInput id="end-host" type="number" formControlName="endHost" />
                </div>
              </div>
              <div class="grid gap-4 sm:grid-cols-2">
                <div class="space-y-1.5">
                  <label hlmLabel for="timeout-ms">Timeout (ms)</label>
                  <input hlmInput id="timeout-ms" type="number" formControlName="timeoutMs" />
                </div>
                <div class="space-y-1.5">
                  <label hlmLabel for="max-concurrency">Max Concurrency</label>
                  <input hlmInput id="max-concurrency" type="number" formControlName="maxConcurrency" />
                </div>
              </div>
            </div>
            <div hlmCardFooter>
              <button hlmBtn type="submit" class="w-full" [disabled]="isScanning()">
                @if (isScanning()) {
                  <app-icon name="loader-circle" [size]="16" class="animate-spin"></app-icon>
                  Scanning...
                } @else {
                  <app-icon name="radio" [size]="16"></app-icon>
                  Scan Subnet
                }
              </button>
            </div>
          </form>
        </div>

        <div hlmCard>
          <div hlmCardHeader>
            <h3 hlmCardTitle>Scan Results</h3>
            <p hlmCardDescription>{{ results().length }} host(s) found.</p>
          </div>

          <div class="divide-y">
            @for (result of results(); track result.ipAddress) {
              <div
                class="flex cursor-pointer items-center justify-between px-6 py-4 transition-colors hover:bg-muted/50 active:bg-muted/70"
                [class.opacity-40]="addingIps().has(result.ipAddress)"
                (click)="saveAsDevice(result)"
              >
                <div class="min-w-0 flex-1">
                  <div class="flex items-center gap-2">
                    <span class="h-2 w-2 rounded-full" [class.bg-success]="result.isOnline" [class.bg-muted]="!result.isOnline"></span>
                    <p class="font-medium truncate">{{ result.ipAddress }}</p>
                  </div>
                  <p class="text-muted-foreground mt-0.5 text-xs truncate">{{ result.hostName || result.summary || 'Unknown host' }}</p>
                </div>
                <div class="flex items-center gap-4">
                  <div class="text-end">
                    <p class="text-xs text-muted-foreground">{{ result.responseTimeMs }} ms</p>
                    <p class="text-xs text-muted-foreground">{{ result.macAddress || 'No MAC' }}</p>
                  </div>
                  @if (addingIps().has(result.ipAddress)) {
                    <app-icon name="check-circle-2" size="16" class="text-success" />
                  } @else {
                    <app-icon name="plus" size="14" class="text-muted-foreground" />
                  }
                </div>
              </div>
            } @empty {
              @if (!isScanning()) {
                <div class="flex flex-col items-center gap-2 py-16 text-sm text-muted-foreground">
                  <app-icon name="radio" [size]="32"></app-icon>
                  <p>Configure and run a scan</p>
                  <p class="text-xs">Results will appear here</p>
                </div>
              } @else {
                <div class="flex flex-col items-center gap-2 py-16 text-sm text-muted-foreground">
                  <app-icon name="loader-circle" [size]="32" class="animate-spin"></app-icon>
                  <p>Scanning network...</p>
                </div>
              }
            }
          </div>
        </div>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ScannerPageComponent {
  private readonly api = inject(ControlApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly toast = inject(ToastService);

  readonly results = signal<NetworkScanResult[]>([]);
  readonly message = signal('');
  readonly isScanning = signal(false);
  readonly addingIps = signal(new Set<string>());

  readonly scanForm = this.formBuilder.group({
    subnetPrefix: '192.168.1',
    startHost: 1,
    endHost: 40,
    timeoutMs: 700,
    maxConcurrency: 24
  });

  scan(): void {
    this.isScanning.set(true);
    this.message.set('');
    this.results.set([]);
    this.addingIps.set(new Set());

    this.api.scanNetwork(this.scanForm.getRawValue()).pipe(finalize(() => this.isScanning.set(false))).subscribe({
      next: r => this.results.set(r),
      error: e => this.toast.error(e.error?.detail ?? 'Scan failed.')
    });
  }

  saveAsDevice(result: NetworkScanResult): void {
    if (this.addingIps().has(result.ipAddress)) return;

    this.addingIps.update(s => new Set(s).add(result.ipAddress));

    const request: DeviceUpsertRequest = {
      name: result.hostName || result.ipAddress,
      ipAddress: result.ipAddress,
      macAddress: result.macAddress,
      broadcastAddress: '255.255.255.255',
      wolPort: 9,
      sshHost: result.ipAddress,
      sshPort: 22,
      sshUsername: '',
      sshPassword: '',
      operatingSystemType: 'Linux',
      autoStartEnabled: true,
      autoShutdownEnabled: true,
      manualControlEnabled: true,
      timeoutSeconds: 15,
      retryCount: 1,
      description: `Imported from scan: ${result.ipAddress}`,
      isActive: true
    };

    this.api.createDevice(request).subscribe({
      next: () => {
        this.toast.success(`Added ${result.ipAddress} as a device.`);
      },
      error: e => {
        this.addingIps.update(s => { const n = new Set(s); n.delete(result.ipAddress); return n; });
        this.toast.error(e.error?.detail ?? 'Failed to save host.');
      }
    });
  }
}
