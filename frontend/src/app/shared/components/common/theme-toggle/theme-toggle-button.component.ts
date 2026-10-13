import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ThemeService } from '../../../services/theme.service';

@Component({
  selector: 'app-theme-toggle-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './theme-toggle-button.component.html',
})
export class ThemeToggleButtonComponent {
  protected readonly themeService = inject(ThemeService);
}
