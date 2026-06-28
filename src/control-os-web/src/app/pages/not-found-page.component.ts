import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../shared/components/icon.component';
import { HlmButton } from '@spartan-ng/helm/button';

@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [RouterLink, IconComponent, HlmButton],
  template: `
    <div class="flex min-h-svh flex-col items-center justify-center gap-6 bg-background p-6 text-center">
      <div class="flex flex-col items-center gap-3">
        <div class="rounded-full bg-muted p-5">
          <app-icon name="file-question" [size]="40" class="text-muted-foreground"></app-icon>
        </div>
        <h1 class="text-6xl font-bold tracking-tight">404</h1>
        <h2 class="text-xl font-semibold">Page not found</h2>
        <p class="max-w-sm text-sm text-muted-foreground">
          The page you're looking for doesn't exist or has been moved.
        </p>
      </div>
      <a hlmBtn routerLink="/dashboard">
        <app-icon name="arrow-left" [size]="16"></app-icon>
        Back to Dashboard
      </a>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundPageComponent {}
