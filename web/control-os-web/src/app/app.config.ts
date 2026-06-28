import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, ErrorHandler } from '@angular/core';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { apiErrorInterceptor } from './core/api-error.interceptor';
import { GlobalErrorHandler } from './core/global-error.handler';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptors([apiErrorInterceptor])),
    provideRouter(
      routes,
      withInMemoryScrolling({
        anchorScrolling: 'enabled',
        scrollPositionRestoration: 'enabled'
      })
    ),
    { provide: ErrorHandler, useClass: GlobalErrorHandler }
  ]
};
