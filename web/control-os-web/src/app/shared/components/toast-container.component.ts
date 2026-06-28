import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../services/toast.service';
import { IconComponent } from './icon.component';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [IconComponent],
  template: `
    <div class="fixed bottom-4 end-4 z-50 flex flex-col gap-2">
      @for (toast of toastService.toasts(); track toast.id) {
        <div
          class="animate-slide-up flex items-center gap-2 rounded-lg border px-4 py-3 text-sm shadow-lg backdrop-blur-sm"
          [class.border-green-500/30]="toast.type === 'success'"
          [class.bg-green-500/10]="toast.type === 'success'"
          [class.text-green-400]="toast.type === 'success'"
          [class.border-red-500/30]="toast.type === 'error'"
          [class.bg-red-500/10]="toast.type === 'error'"
          [class.text-red-400]="toast.type === 'error'"
          [class.border-blue-500/30]="toast.type === 'info'"
          [class.bg-blue-500/10]="toast.type === 'info'"
          [class.text-blue-400]="toast.type === 'info'"
        >
          @switch (toast.type) {
            @case ('success') { <app-icon name="circle-check" [size]="16"></app-icon> }
            @case ('error') { <app-icon name="circle-x" [size]="16"></app-icon> }
            @case ('info') { <app-icon name="info" [size]="16"></app-icon> }
          }
          {{ toast.message }}
        </div>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ToastContainerComponent {
  protected readonly toastService = inject(ToastService);
}
