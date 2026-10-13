import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-label',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './label.component.html',
})
export class LabelComponent {
  readonly for = input<string>();
  readonly className = input<string>('');
}
