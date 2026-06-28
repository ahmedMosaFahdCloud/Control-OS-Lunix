import { Injectable, signal, effect, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'control-os-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly platformId = inject(PLATFORM_ID);
  readonly theme = signal<Theme>('light');

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      const stored = localStorage.getItem(STORAGE_KEY) as Theme | null;
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      const initial = stored ?? (prefersDark ? 'dark' : 'light');
      this.theme.set(initial);
      this.applyTheme(initial);

      effect(() => {
        const t = this.theme();
        localStorage.setItem(STORAGE_KEY, t);
        this.applyTheme(t);
      });
    }
  }

  toggle(): void {
    this.theme.update(t => (t === 'light' ? 'dark' : 'light'));
  }

  private applyTheme(t: Theme): void {
    document.documentElement.classList.toggle('dark', t === 'dark');
    document.documentElement.style.colorScheme = t;
  }
}
