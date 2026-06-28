import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

const BOOTSTRAP_MAP: Record<string, string> = {
  'layout-dashboard': 'bi-grid-1x2',
  monitor: 'bi-pc-display',
  radio: 'bi-broadcast',
  logs: 'bi-journal-text',
  settings: 'bi-gear',
  'refresh-cw': 'bi-arrow-clockwise',
  save: 'bi-save',
  'circle-check': 'bi-check-circle',
  'circle-x': 'bi-x-circle',
  info: 'bi-info-circle',
  target: 'bi-bullseye',
  activity: 'bi-activity',
  circle: 'bi-circle',
  search: 'bi-search',
  plus: 'bi-plus-lg',
  'trash-2': 'bi-trash',
  play: 'bi-play-fill',
  'power-off': 'bi-power',
  x: 'bi-x',
  menu: 'bi-list',
  'external-link': 'bi-box-arrow-up-right',
  'arrow-left': 'bi-arrow-left',
  'loader-circle': 'bi-arrow-repeat',
  'check-circle-2': 'bi-check-circle-fill',
  sun: 'bi-sun',
  moon: 'bi-moon',
  languages: 'bi-translate',
  cpu: 'bi-cpu',
};

@Component({
  selector: 'app-icon',
  standalone: true,
  template: `<i class="bi {{ bootstrapClass() }}" [style.fontSize.px]="pixelSize()"></i>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IconComponent {
  readonly name = input.required<string>();
  readonly size = input<number | string>(16);
  readonly pixelSize = computed(() => Number(this.size()));
  readonly bootstrapClass = computed(() => BOOTSTRAP_MAP[this.name()] ?? 'bi-question-circle');
}
