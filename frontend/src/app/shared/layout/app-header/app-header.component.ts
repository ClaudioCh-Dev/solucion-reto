import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { BrandLogoComponent } from '../../components/common/brand-logo/brand-logo.component';
import { ThemeToggleButtonComponent } from '../../components/common/theme-toggle/theme-toggle-button.component';
import { UserDropdownComponent } from '../../components/header/user-dropdown/user-dropdown.component';
import { SidebarService } from '../../services/sidebar.service';

@Component({
  selector: 'app-header',
  imports: [RouterLink, BrandLogoComponent, ThemeToggleButtonComponent, UserDropdownComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app-header.component.html',
})
export class AppHeaderComponent {
  protected readonly sidebar = inject(SidebarService);
  protected readonly isApplicationMenuOpen = signal(false);

  protected handleToggle(): void {
    if (window.innerWidth >= 1280) this.sidebar.toggleExpanded();
    else this.sidebar.toggleMobileOpen();
  }
}
