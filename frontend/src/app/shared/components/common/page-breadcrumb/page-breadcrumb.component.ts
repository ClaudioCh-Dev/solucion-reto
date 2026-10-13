import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-page-breadcrumb',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './page-breadcrumb.component.html',
})
export class PageBreadcrumbComponent {
  readonly pageTitle = input<string>('');
}
