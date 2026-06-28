import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ApiStatusBannerComponent } from './shared/components/api-status-banner.component';
import { ToastContainerComponent } from './shared/components/toast-container.component';
import { AppSidebar } from './shared/sidebar/app-sidebar';
import { SiteHeader } from './shared/sidebar/site-header';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ApiStatusBannerComponent, ToastContainerComponent, AppSidebar, SiteHeader],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {}
