import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { toUserMessage } from '../../../core/utils/api-error';
import { PageBreadcrumbComponent } from '../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';
import { AlertComponent } from '../../../shared/components/ui/alert/alert.component';
import { BadgeComponent } from '../../../shared/components/ui/badge/badge.component';
import { ICONS } from '../../../shared/icons/icons';
import { SafeHtmlPipe } from '../../../shared/pipe/safe-html.pipe';
import { ProductsApi } from '../services/products.api';

@Component({
  selector: 'app-product-list-page',
  imports: [AlertComponent, BadgeComponent, CurrencyPipe, PageBreadcrumbComponent, RouterLink, SafeHtmlPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-list.page.html',
})
export class ProductListPage {
  private readonly api = inject(ProductsApi);
  protected readonly icons = ICONS;

  /** Catálogo desde la base local. Se vuelve a pedir cada vez que se entra a la página. */
  protected readonly products = rxResource({
    stream: () => this.api.list(),
    defaultValue: [],
  });

  protected readonly errorMessage = computed(() => {
    const error = this.products.error();
    return error ? toUserMessage(error) : null;
  });
}
