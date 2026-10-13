import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ThemeService } from '../../../services/theme.service';

@Component({
  selector: 'app-theme-toggle-two',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './theme-toggle-two.component.html',
})
export class ThemeToggleTwoComponent {
  protected readonly themeService = inject(ThemeService);
}
