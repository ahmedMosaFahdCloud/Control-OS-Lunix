import { ErrorHandler, inject, Injectable } from '@angular/core';
import { ToastService } from '../shared/services/toast.service';

const UNEXPECTED_ERROR = 'An unexpected error occurred.';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly toast = inject(ToastService, { optional: true });

  handleError(error: unknown): void {
    const message = error instanceof Error ? error.message : UNEXPECTED_ERROR;
    console.error('[GlobalErrorHandler]', error);
    this.toast?.error(message);
  }
}
