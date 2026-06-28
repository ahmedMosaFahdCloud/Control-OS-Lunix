import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SidebarState {
  readonly expanded = signal(true);
}
