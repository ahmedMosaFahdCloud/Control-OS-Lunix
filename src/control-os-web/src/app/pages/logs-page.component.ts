import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { interval } from 'rxjs';
import { ControlApiService } from '../core/control-api.service';
import { API, ERROR_MSGS } from '../core/constants';
import { IconComponent } from '../shared/components/icon.component';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmCard } from '@spartan-ng/helm/card';
import { HlmSkeleton } from '@spartan-ng/helm/skeleton';
import { HlmSwitch } from '@spartan-ng/helm/switch';

@Component({
  selector: 'app-logs-page',
  standalone: true,
  imports: [ReactiveFormsModule, IconComponent,
    HlmButton, HlmInput, HlmCard, HlmSkeleton, HlmSwitch],
  template: `
    <div class="space-y-6">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 class="text-2xl font-semibold tracking-tight">System Logs</h1>
          <p class="text-muted-foreground mt-1 text-sm">Controller events and device activity stream.</p>
        </div>
        <div class="flex items-center gap-3">
          <label class="flex items-center gap-2 text-sm text-muted-foreground">
            <hlm-switch [checked]="autoRefresh()" (checkedChange)="toggleAutoRefresh()" />
            Auto-refresh
          </label>
          <button hlmBtn variant="outline" (click)="load()">
            <app-icon name="refresh-cw" [size]="14"></app-icon>
            Reload
          </button>
        </div>
      </div>

      <div hlmCard>
        <div class="flex flex-wrap items-center justify-between gap-3 border-b px-6 py-3">
          <div class="flex items-center gap-3">
            <span class="text-xs font-medium text-muted-foreground">Lines: {{ lines().length }}</span>
          </div>
          <div class="flex items-center gap-2">
            <input hlmInput class="w-48" placeholder="Filter logs..." [formControl]="filterControl" />
            <select hlmInput class="w-auto" [formControl]="lineCountControl">
              @for (opt of API.SCAN_LINE_OPTIONS; track opt) {
                <option [value]="opt">{{ opt }} lines</option>
              }
            </select>
          </div>
        </div>

        <div class="max-h-[600px] overflow-y-auto bg-[#1a1a2e] p-4 font-mono text-xs leading-relaxed">
          @if (lines().length === 0 && !loading()) {
            <div class="flex flex-col items-center gap-2 py-16 text-sm text-muted-foreground">
              <app-icon name="logs" [size]="32"></app-icon>
              <p>No log entries available</p>
            </div>
          } @else if (filteredLines().length === 0 && lines().length > 0) {
            <div class="flex flex-col items-center gap-2 py-12 text-sm text-muted-foreground">
              <app-icon name="search" [size]="24"></app-icon>
              <p>No logs match your filter</p>
            </div>
          } @else if (!loading()) {
            @for (line of filteredLines(); track $index) {
              <div class="border-b border-white/5 py-1.5 last:border-0">
                <span class="text-emerald-400">$</span>
                <span class="text-gray-300">{{ line }}</span>
              </div>
            }
          }
          @if (loading()) {
            <div class="space-y-2 p-4">
              @for (_ of API.SKELETON_SIX; track $index) {
                <div hlmSkeleton class="h-4 w-full"></div>
              }
            </div>
          }
        </div>

        <div class="flex items-center justify-between border-t px-6 py-2.5">
          <p class="text-xs text-muted-foreground">Showing {{ filteredLines().length }} of {{ lines().length }} entries</p>
          <button hlmBtn variant="outline" size="xs" (click)="clearLogs()">Clear</button>
        </div>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogsPageComponent {
  private readonly api = inject(ControlApiService);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly destroyRef = inject(DestroyRef);

  protected readonly API = API;

  readonly lines = signal<string[]>([]);
  readonly filteredLines = signal<string[]>([]);
  readonly autoRefresh = signal(false);
  readonly loading = signal(false);
  readonly filterControl = this.formBuilder.control('');
  readonly lineCountControl = this.formBuilder.control(API.SCAN_DEFAULT_LINES);

  constructor() {
    this.load();

    this.filterControl.valueChanges.subscribe(() => this.applyFilter());
    this.lineCountControl.valueChanges.subscribe(() => this.load());

    const sub = interval(API.LOGS_REFRESH_MS).subscribe(() => { if (this.autoRefresh()) this.load(); });
    this.destroyRef.onDestroy(() => sub.unsubscribe());
  }

  toggleAutoRefresh(): void { this.autoRefresh.update(v => !v); }

  load(): void {
    this.loading.set(true);
    this.api.getLogs(this.lineCountControl.value ?? API.SCAN_DEFAULT_LINES).subscribe({
      next: r => { this.lines.set(r.lines); this.applyFilter(); this.loading.set(false); },
      error: () => { this.lines.set([`Error: ${ERROR_MSGS.LOAD_LOGS}`]); this.loading.set(false); }
    });
  }

  clearLogs(): void {
    this.lines.set([]);
    this.filteredLines.set([]);
  }

  private applyFilter(): void {
    const q = (this.filterControl.value ?? '').toLowerCase();
    this.filteredLines.set(q ? this.lines().filter(l => l.toLowerCase().includes(q)) : [...this.lines()]);
  }
}
