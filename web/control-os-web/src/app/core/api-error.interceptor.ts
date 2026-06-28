import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, tap, timeout, throwError } from 'rxjs';
import { ApiStatusService } from '../shared/services/api-status.service';
import { ToastService } from '../shared/services/toast.service';

export const apiErrorInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const apiStatus = inject(ApiStatusService);
  const toast = inject(ToastService);

  return next(req).pipe(
    timeout(15_000),
    tap(() => apiStatus.markOnline()),
    catchError((error: unknown) => {
      const isConnectionError = error instanceof HttpErrorResponse
        ? error.status === 0
        : error instanceof Error && error.name === 'TimeoutError';

      if (isConnectionError) {
        const message = 'Cannot reach the API server. Check if the backend is running.';
        apiStatus.markOffline(message);
        toast.error(message);
      } else {
        apiStatus.markOnline();
      }
      return throwError(() => error);
    })
  );
};
