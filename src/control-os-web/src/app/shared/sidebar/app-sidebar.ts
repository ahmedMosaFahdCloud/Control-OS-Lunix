import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { IconComponent } from '../components/icon.component';
import { NavMain } from './nav-main';
import { data } from './data';
import { SidebarState } from './sidebar-state';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';

@Component({
  selector: 'spartan-app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, IconComponent, NavMain, ...HlmButtonImports, ...HlmSeparatorImports],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex min-h-screen bg-background text-foreground">
      <aside
        class="hidden flex-col gap-4 border-r border-sidebar-border bg-sidebar px-2 py-4 transition-all duration-200 md:flex"
        [class.w-64]="state.expanded()"
        [class.w-16]="!state.expanded()"
      >
        <!-- Logo -->
        <a
          routerLink="/dashboard"
          class="group flex items-center gap-3 rounded-xl border border-sidebar-border/50 bg-sidebar-accent/20 px-3 py-2.5 text-sidebar-foreground transition hover:border-sidebar-primary/30 hover:bg-sidebar-accent/40"
          [class.justify-center]="!state.expanded()"
        >
          <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-sidebar-primary text-sidebar-primary-foreground">
            <app-icon name="cpu" size="18" />
          </div>
          @if (state.expanded()) {
            <div class="overflow-hidden">
              <p class="text-sm font-semibold leading-tight">Control OS</p>
              <p class="text-xs text-sidebar-foreground/60">Device Manager</p>
            </div>
          }
        </a>

        <div hlmSeparator></div>

        <!-- Navigation -->
        <nav class="flex-1 space-y-1 overflow-auto">
          <spartan-nav-main [items]="data.navMain" [expanded]="state.expanded()" />
        </nav>

        <div hlmSeparator></div>

        <!-- Secondary links -->
        <div class="flex flex-col gap-1">
          <a
            routerLink="/logs"
            routerLinkActive="bg-sidebar-accent/80 text-sidebar-foreground"
            class="flex items-center gap-3 rounded-xl px-3 py-2 text-sm font-medium text-sidebar-foreground/70 transition-colors hover:bg-sidebar-accent/50 hover:text-sidebar-foreground"
            [class.justify-center]="!state.expanded()"
            [title]="state.expanded() ? '' : 'Logs'"
          >
            <app-icon name="logs" size="16" />
            @if (state.expanded()) { <span>Logs</span> }
          </a>
          <a
            routerLink="/settings"
            routerLinkActive="bg-sidebar-accent/80 text-sidebar-foreground"
            class="flex items-center gap-3 rounded-xl px-3 py-2 text-sm font-medium text-sidebar-foreground/70 transition-colors hover:bg-sidebar-accent/50 hover:text-sidebar-foreground"
            [class.justify-center]="!state.expanded()"
            [title]="state.expanded() ? '' : 'Settings'"
          >
            <app-icon name="settings" size="16" />
            @if (state.expanded()) { <span>Settings</span> }
          </a>
        </div>
      </aside>

      <div class="flex min-h-screen flex-1 flex-col">
        <ng-content />
      </div>
    </div>
  `,
})
export class AppSidebar {
  protected readonly data = data;
  protected readonly state = inject(SidebarState);
}
