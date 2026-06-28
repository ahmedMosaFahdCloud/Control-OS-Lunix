import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ApiStatusService } from '../services/api-status.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { IconComponent } from './icon.component';

@Component({
  selector: 'app-api-status-banner',
  standalone: true,
  imports: [IconComponent],
  template: `
    @if (!apiStatus.isOnline()) {
      <div class="fixed bottom-4 inset-x-0 z-50 flex justify-center px-4">
        <div class="animate-slide-up flex items-center gap-3 rounded-lg border border-red-500/30 bg-red-500/10 px-5 py-3.5 text-sm text-red-400 shadow-lg backdrop-blur-sm">
          <app-icon name="circle-x" [size]="18" class="shrink-0"></app-icon>
          <div class="flex flex-col">
            <span class="font-medium">{{ i18n.t('api.offline.title') }}</span>
            <span class="text-red-400/70">{{ i18n.t('api.offline.message') }}</span>
          </div>
          <button
            class="shrink-0 rounded-md border border-red-500/30 px-3 py-1 text-xs font-medium transition-colors hover:bg-red-500/20"
            (click)="retry()"
          >
            {{ i18n.t('api.offline.retry') }}
          </button>
        </div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ApiStatusBannerComponent {
  protected readonly apiStatus = inject(ApiStatusService);
  protected readonly i18n = inject(I18nService);

  retry(): void {
    this.apiStatus.markOnline();
    window.location.reload();
  }
}
