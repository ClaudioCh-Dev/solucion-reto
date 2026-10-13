import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { ToastOutletComponent } from './shared/components/common/toast-outlet/toast-outlet.component';
import { ThemeService } from './shared/services/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastOutletComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <router-outlet />
    <app-toast-outlet />
  `,
})
export class App {
  constructor() {
    inject(ThemeService); // aplica el tema guardado (claro/oscuro) al arrancar
  }
}
