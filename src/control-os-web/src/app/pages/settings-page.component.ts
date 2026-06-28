import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ControlApiService } from '../core/control-api.service';
import { GlobalSettings } from '../core/api.models';
import { DEFAULTS, ERROR_MSGS } from '../core/constants';
import { environment } from '../../environments/environment';
import { IconComponent } from '../shared/components/icon.component';
import { ToastService } from '../shared/services/toast.service';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmLabel } from '@spartan-ng/helm/label';
import { HlmSwitch } from '@spartan-ng/helm/switch';
import { HlmCard, HlmCardHeader, HlmCardTitle, HlmCardDescription, HlmCardContent, HlmCardFooter } from '@spartan-ng/helm/card';

const FORM_DEFAULTS = {
  autoStartDevicesOnControllerBoot: DEFAULTS.SETTINGS_AUTO_START_BOOT,
  autoShutdownDevicesOnControllerShutdown: DEFAULTS.SETTINGS_AUTO_SHUTDOWN_STOP,
  delayBetweenCommandsMs: DEFAULTS.SETTINGS_DELAY_MS,
  pingTimeoutSeconds: DEFAULTS.SETTINGS_PING_S,
  sshTimeoutSeconds: DEFAULTS.SETTINGS_SSH_S,
  retryCount: DEFAULTS.SETTINGS_RETRY,
  defaultWolPort: DEFAULTS.WOL_PORT,
  defaultBroadcastAddress: DEFAULTS.BROADCAST_ADDRESS,
  enableLogs: DEFAULTS.SETTINGS_ENABLE_LOGS,
  logRetentionDays: DEFAULTS.SETTINGS_LOG_DAYS,
  confirmManualShutdown: DEFAULTS.SETTINGS_CONFIRM_SHUTDOWN,
} as const;

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [ReactiveFormsModule, IconComponent,
    HlmButton, HlmInput, HlmLabel, HlmSwitch,
    HlmCard, HlmCardHeader, HlmCardTitle, HlmCardDescription, HlmCardContent, HlmCardFooter],
  template: `
    <div class="space-y-6">
      <div>
        <h1 class="text-2xl font-semibold tracking-tight">Settings</h1>
        <p class="text-muted-foreground mt-1 text-sm">Controller behavior, timeouts, and data management.</p>
      </div>

      <div class="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <div class="space-y-6">
          <div hlmCard>
            <div hlmCardHeader>
              <div class="flex items-center gap-2">
                <app-icon name="activity" [size]="18" class="text-primary"></app-icon>
                <h3 hlmCardTitle>Automation</h3>
              </div>
              <p hlmCardDescription>Control automatic device behavior on controller events.</p>
            </div>
            <div hlmCardContent class="space-y-4" [formGroup]="form">
              @for (item of automationToggles; track item.key) {
                <label class="flex items-center justify-between gap-3 text-sm">
                  <div>
                    <p class="font-medium">{{ item.label }}</p>
                    <p class="text-muted-foreground text-xs">{{ item.desc }}</p>
                  </div>
                  <hlm-switch [formControlName]="item.key" />
                </label>
              }
            </div>
          </div>

          <div hlmCard>
            <div hlmCardHeader>
              <div class="flex items-center gap-2">
                <app-icon name="settings" [size]="18" class="text-primary"></app-icon>
                <h3 hlmCardTitle>Timeouts & Limits</h3>
              </div>
              <p hlmCardDescription>Network and SSH operation parameters.</p>
            </div>
            <form [formGroup]="form" (ngSubmit)="save()">
              <div hlmCardContent class="space-y-4">
                <div class="grid gap-4 sm:grid-cols-3">
                  <div class="space-y-1.5">
                    <label hlmLabel for="delay-ms">Delay (ms)</label>
                    <input hlmInput id="delay-ms" type="number" formControlName="delayBetweenCommandsMs" />
                  </div>
                  <div class="space-y-1.5">
                    <label hlmLabel for="ping-timeout">Ping (s)</label>
                    <input hlmInput id="ping-timeout" type="number" formControlName="pingTimeoutSeconds" />
                  </div>
                  <div class="space-y-1.5">
                    <label hlmLabel for="ssh-timeout">SSH (s)</label>
                    <input hlmInput id="ssh-timeout" type="number" formControlName="sshTimeoutSeconds" />
                  </div>
                  <div class="space-y-1.5">
                    <label hlmLabel for="retry-count">Retries</label>
                    <input hlmInput id="retry-count" type="number" formControlName="retryCount" />
                  </div>
                  <div class="space-y-1.5">
                    <label hlmLabel for="wol-port">WOL Port</label>
                    <input hlmInput id="wol-port" type="number" formControlName="defaultWolPort" />
                  </div>
                  <div class="space-y-1.5">
                    <label hlmLabel for="log-days">Log Retention</label>
                    <input hlmInput id="log-days" type="number" formControlName="logRetentionDays" />
                  </div>
                </div>
                <div class="space-y-1.5">
                  <label hlmLabel for="broadcast-address">Default Broadcast Address</label>
                  <input hlmInput id="broadcast-address" formControlName="defaultBroadcastAddress" class="font-mono" />
                </div>
              </div>
              <div hlmCardFooter>
                <button hlmBtn type="submit">Save Settings</button>
              </div>
            </form>
          </div>
        </div>

        <div class="space-y-6">
          <div hlmCard>
            <div hlmCardHeader>
              <div class="flex items-center gap-2">
                <app-icon name="save" [size]="18" class="text-primary"></app-icon>
                <h3 hlmCardTitle>Backup & Restore</h3>
              </div>
              <p hlmCardDescription>Create and restore configuration archives.</p>
            </div>
            <div hlmCardContent class="space-y-4">
              @if (message()) {
                <div class="rounded-lg border border-success/30 bg-success/10 px-4 py-3 text-sm text-success">{{ message() }}</div>
              }

              <button hlmBtn variant="outline" class="w-full" (click)="backup()">
                <app-icon name="save" [size]="16"></app-icon>
                Create Backup
              </button>

              <div class="space-y-1.5">
                <label hlmLabel for="archive-path">Restore from Path</label>
                <input hlmInput id="archive-path" [formControl]="restorePathControl" class="font-mono" placeholder="/path/to/backup.zip" />
              </div>
              <button hlmBtn variant="outline" class="w-full" (click)="restore()">
                <app-icon name="refresh-cw" [size]="16"></app-icon>
                Restore Backup
              </button>
            </div>
          </div>

          <div hlmCard>
            <div hlmCardHeader>
              <div class="flex items-center gap-2">
                <app-icon name="info" [size]="18" class="text-primary"></app-icon>
                <h3 hlmCardTitle>System Info</h3>
              </div>
            </div>
            <div hlmCardContent class="space-y-3 text-sm">
              <div class="flex justify-between"><span class="text-muted-foreground">API</span><span class="font-mono text-xs">{{ apiBaseUrl }}</span></div>
              <div class="flex justify-between"><span class="text-muted-foreground">Frontend</span><span class="font-mono text-xs">Angular + Tailwind</span></div>
              <div class="flex justify-between"><span class="text-muted-foreground">Auth</span><span class="font-mono text-xs">None (local)</span></div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsPageComponent {
  private readonly api = inject(ControlApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly toast = inject(ToastService);

  readonly apiBaseUrl = environment.apiBaseUrl;

  readonly message = signal('');
  readonly restorePathControl = this.formBuilder.control('');

  readonly automationToggles = [
    { key: 'autoStartDevicesOnControllerBoot' as const, label: 'Auto-start on boot', desc: 'Start all devices when the controller starts.' },
    { key: 'autoShutdownDevicesOnControllerShutdown' as const, label: 'Auto-shutdown on stop', desc: 'Shut down devices when the controller stops.' },
    { key: 'enableLogs' as const, label: 'Enable logging', desc: 'Record controller and device events.' },
    { key: 'confirmManualShutdown' as const, label: 'Confirm shutdown', desc: 'Require confirmation before shutdown operations.' }
  ];

  readonly form = this.formBuilder.group(FORM_DEFAULTS);

  constructor() {
    this.api.getSettings().subscribe({
      next: s => this.form.reset(s as unknown as Record<string, unknown>),
      error: () => this.toast.error(ERROR_MSGS.LOAD_SETTINGS)
    });
  }

  save(): void {
    this.api.saveSettings(this.form.getRawValue() as GlobalSettings).subscribe({
      next: () => this.toast.success('Settings saved.'),
      error: () => this.toast.error(ERROR_MSGS.SAVE_SETTINGS)
    });
  }

  backup(): void {
    this.api.createBackup().subscribe({
      next: r => { this.restorePathControl.setValue(r.archivePath); this.message.set(r.message); this.toast.success('Backup created.'); },
      error: () => this.toast.error(ERROR_MSGS.BACKUP)
    });
  }

  restore(): void {
    this.api.restoreBackup(this.restorePathControl.getRawValue()).subscribe({
      next: r => this.toast.success(r.message),
      error: () => this.toast.error(ERROR_MSGS.RESTORE)
    });
  }
}
