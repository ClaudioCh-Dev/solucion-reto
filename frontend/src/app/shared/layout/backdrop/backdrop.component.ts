import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { SidebarService } from '../../services/sidebar.service';

@Component({
  selector: 'app-backdrop',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './backdrop.component.html',
})
export class BackdropComponent {
  protected readonly sidebar = inject(SidebarService);
}
