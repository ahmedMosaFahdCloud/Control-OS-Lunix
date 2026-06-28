import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ApiStatusService {
  readonly isOnline = signal(true);
  readonly lastError = signal<string | null>(null);

  markOffline(error: string): void {
    this.isOnline.set(false);
    this.lastError.set(error);
  }

  markOnline(): void {
    this.isOnline.set(true);
    this.lastError.set(null);
  }
}
