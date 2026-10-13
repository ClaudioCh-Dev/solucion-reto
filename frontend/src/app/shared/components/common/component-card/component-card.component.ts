import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-component-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './component-card.component.html',
})
export class ComponentCardComponent {
  readonly title = input<string>('');
  readonly desc = input<string>('');
  readonly className = input<string>('');
}
