import { ChangeDetectionStrategy, Component } from '@angular/core';

import { BrandLogoComponent } from '../../components/common/brand-logo/brand-logo.component';
import { GridShapeComponent } from '../../components/common/grid-shape/grid-shape.component';
import { ThemeToggleTwoComponent } from '../../components/common/theme-toggle-two/theme-toggle-two.component';

/** Layout de pantallas sin sesión (login): formulario a la izquierda y panel de marca a la derecha. */
@Component({
  selector: 'app-auth-page-layout',
  imports: [BrandLogoComponent, GridShapeComponent, ThemeToggleTwoComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './auth-page-layout.component.html',
})
export class AuthPageLayoutComponent {}
