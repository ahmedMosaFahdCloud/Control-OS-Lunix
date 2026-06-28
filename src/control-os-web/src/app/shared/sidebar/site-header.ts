import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { IconComponent } from '../components/icon.component';
import { ThemeService } from '../services/theme.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { SidebarState } from './sidebar-state';
import { HlmButtonImports } from '@spartan-ng/helm/button';

@Component({
  selector: 'spartan-site-header',
  standalone: true,
  imports: [IconComponent, ...HlmButtonImports],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="sticky top-0 z-30 border-b border-border/40 bg-background/80 px-4 py-3 backdrop-blur-sm">
      <div class="flex items-center justify-between gap-4">
        <div class="flex items-center gap-3">
          <button hlmBtn variant="ghost" size="icon-sm" (click)="sidebar.expanded.update(v => !v)" class="hidden md:inline-flex">
            @if (sidebar.expanded()) {
              <app-icon name="arrow-left" size="16" />
            } @else {
              <app-icon name="menu" size="16" />
            }
          </button>
          <div>
            <h1 class="text-lg font-semibold tracking-tight text-foreground">Control OS</h1>
            <p class="text-xs text-muted-foreground">Device Management Controller</p>
          </div>
        </div>
        <div class="flex items-center gap-1">
          <button hlmBtn variant="ghost" size="icon-sm" (click)="i18n.toggle()" [attr.aria-label]="'Toggle language'">
            <app-icon name="languages" size="16" />
          </button>
          <button hlmBtn variant="ghost" size="icon-sm" (click)="theme.toggle()" [attr.aria-label]="'Toggle theme'">
            @if (theme.theme() === 'dark') {
              <app-icon name="sun" size="16" />
            } @else {
              <app-icon name="moon" size="16" />
            }
          </button>
        </div>
      </div>
    </header>
  `,
})
export class SiteHeader {
  protected readonly sidebar = inject(SidebarState);
  protected readonly theme = inject(ThemeService);
  protected readonly i18n = inject(I18nService);
}
