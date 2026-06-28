import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, tap, timeout, throwError } from 'rxjs';
import { API, ERROR_MSGS } from './constants';
import { ApiStatusService } from '../shared/services/api-status.service';
import { ToastService } from '../shared/services/toast.service';

export const apiErrorInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const apiStatus = inject(ApiStatusService);
  const toast = inject(ToastService);

  return next(req).pipe(
    timeout(API.TIMEOUT_MS),
    tap(() => apiStatus.markOnline()),
    catchError((error: unknown) => {
      const isConnectionError = error instanceof HttpErrorResponse
        ? error.status === 0
        : error instanceof Error && error.name === 'TimeoutError';

      if (isConnectionError) {
        apiStatus.markOffline(ERROR_MSGS.OFFLINE);
        toast.error(ERROR_MSGS.OFFLINE);
      } else {
        apiStatus.markOnline();
      }
      return throwError(() => error);
    })
  );
};
