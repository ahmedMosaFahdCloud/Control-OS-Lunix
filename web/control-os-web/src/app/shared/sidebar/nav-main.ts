import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { IconComponent } from '../components/icon.component';
import type { NavItem } from './data';

@Component({
  selector: 'spartan-nav-main',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav aria-label="Primary" class="space-y-1">
      @for (item of items(); track item.url) {
        <a
          [routerLink]="item.url"
          routerLinkActive="bg-sidebar-accent/80 text-sidebar-foreground font-semibold"
          [routerLinkActiveOptions]="{ exact: item.url === '/dashboard' }"
          class="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-sidebar-foreground/70 transition-colors hover:bg-sidebar-accent/50 hover:text-sidebar-foreground"
          [class.justify-center]="!expanded()"
          [title]="expanded() ? '' : item.title"
        >
          <app-icon [name]="item.icon" size="16" />
          @if (expanded()) {
            <span>{{ item.title }}</span>
          }
        </a>
      }
    </nav>
  `,
})
export class NavMain {
  public readonly items = input<NavItem[]>([]);
  public readonly expanded = input(true);
}
