import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { SidebarService } from '../../services/sidebar.service';
import { AppHeaderComponent } from '../app-header/app-header.component';
import { AppSidebarComponent } from '../app-sidebar/app-sidebar.component';
import { BackdropComponent } from '../backdrop/backdrop.component';

/** Shell del panel: sidebar + header + contenido de la ruta. */
@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, AppHeaderComponent, AppSidebarComponent, BackdropComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app-layout.component.html',
})
export class AppLayoutComponent {
  protected readonly sidebar = inject(SidebarService);
}
