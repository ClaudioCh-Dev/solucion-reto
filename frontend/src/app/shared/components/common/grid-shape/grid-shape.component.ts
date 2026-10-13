import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-grid-shape',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './grid-shape.component.html',
})
export class GridShapeComponent {}
