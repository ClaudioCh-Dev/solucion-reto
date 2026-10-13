import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { BrandLogoComponent } from '../../components/common/brand-logo/brand-logo.component';
import { ICONS } from '../../icons/icons';
import { SafeHtmlPipe } from '../../pipe/safe-html.pipe';
import { SidebarService } from '../../services/sidebar.service';

interface NavItem {
  name: string;
  icon: string;
  path: string;
}

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, BrandLogoComponent, SafeHtmlPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app-sidebar.component.html',
})
export class AppSidebarComponent {
  protected readonly sidebar = inject(SidebarService);

  protected readonly navItems: NavItem[] = [{ icon: ICONS.box, name: 'Productos', path: '/products' }];

  protected onMouseEnter(): void {
    if (!this.sidebar.isExpanded()) this.sidebar.isHovered.set(true);
  }

  /** En móvil, al elegir una opción se cierra el menú. */
  protected onNavigate(): void {
    this.sidebar.isMobileOpen.set(false);
  }
}
