import { Injectable, signal, computed, effect, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export type Language = 'en' | 'ar';

const STORAGE_KEY = 'control-os-lang';

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly http = inject(HttpClient);
  readonly currentLang = signal<Language>('en');
  readonly dir = computed(() => (this.currentLang() === 'ar' ? 'rtl' : 'ltr'));

  private data: Record<Language, Record<string, string>> = { en: {}, ar: {} };
  private readonly fallbackLang: Language = 'en';

  constructor() {
    const stored = localStorage.getItem(STORAGE_KEY) as Language | null;
    if (stored === 'en' || stored === 'ar') {
      this.currentLang.set(stored);
    }
    this.load();
    effect(() => {
      localStorage.setItem(STORAGE_KEY, this.currentLang());
      document.documentElement.dir = this.dir();
    });
  }

  private async load(): Promise<void> {
    const [en, ar] = await Promise.all([
      firstValueFrom(this.http.get<Record<string, string>>('i18n/en.json')),
      firstValueFrom(this.http.get<Record<string, string>>('i18n/ar.json')),
    ]);
    this.data = { en, ar };
  }

  t(key: string): string {
    const lang = this.currentLang();
    return this.data[lang]?.[key] ?? this.data[this.fallbackLang]?.[key] ?? key;
  }

  toggle(): void {
    this.currentLang.update(l => (l === 'en' ? 'ar' : 'en'));
  }

  setLang(lang: Language): void {
    if (lang in this.data) {
      this.currentLang.set(lang);
    }
  }
}
