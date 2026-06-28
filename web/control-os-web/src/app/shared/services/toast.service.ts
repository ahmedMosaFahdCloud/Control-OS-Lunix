import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info';
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  readonly toasts = signal<Toast[]>([]);

  show(message: string, type: Toast['type'] = 'info', duration = 4000): void {
    const id = this.nextId++;
    this.toasts.update(t => [...t, { id, message, type }]);
    setTimeout(() => this.toasts.update(t => t.filter(x => x.id !== id)), duration);
  }

  success(message: string): void { this.show(message, 'success'); }
  error(message: string): void { this.show(message, 'error'); }
  info(message: string): void { this.show(message, 'info'); }
}
