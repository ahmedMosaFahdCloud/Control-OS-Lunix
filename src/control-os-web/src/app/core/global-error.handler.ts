import { ErrorHandler, inject, Injectable } from '@angular/core';
import { ToastService } from '../shared/services/toast.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly toast = inject(ToastService, { optional: true });

  handleError(error: unknown): void {
    const message = error instanceof Error ? error.message : 'An unexpected error occurred.';
    console.error('[GlobalErrorHandler]', error);
    this.toast?.error(message);
  }
}
