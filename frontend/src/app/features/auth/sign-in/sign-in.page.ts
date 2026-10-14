import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { email, form, FormField, required, submit } from '@angular/forms/signals';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthStore } from '../../../core/services/auth.store';
import { toUserMessage } from '../../../core/utils/api-error';
import { FieldErrorsComponent } from '../../../shared/components/form/field-errors/field-errors.component';
import { InputFieldComponent } from '../../../shared/components/form/input/input-field.component';
import { LabelComponent } from '../../../shared/components/form/label/label.component';
import { AlertComponent } from '../../../shared/components/ui/alert/alert.component';
import { AuthPageLayoutComponent } from '../../../shared/layout/auth-page-layout/auth-page-layout.component';

interface SignInModel {
  email: string;
  password: string;
}

@Component({
  selector: 'app-sign-in-page',
  imports: [
    AuthPageLayoutComponent,
    AlertComponent,
    FieldErrorsComponent,
    FormField,
    InputFieldComponent,
    LabelComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sign-in.page.html',
})
export class SignInPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  /** ?expired=1 cuando se llega por sesión vencida (query param enlazado como input). */
  readonly expired = input<string>();

  protected readonly showPassword = signal(false);
  protected readonly error = signal('');

  private readonly model = signal<SignInModel>({ email: '', password: '' });
  protected readonly signInForm = form(this.model, (p) => {
    required(p.email, { message: 'Ingresa tu correo.' });
    email(p.email, { message: 'Ingresa un correo válido.' });
    required(p.password, { message: 'Ingresa tu contraseña.' });
  });

  protected async onSubmit(event: Event): Promise<void> {
    event.preventDefault();
    this.error.set('');

    await submit(this.signInForm, async () => {
      try {
        await firstValueFrom(this.auth.login(this.model()));
        await this.router.navigateByUrl('/products');
      } catch (err) {
        // El backend ya manda el texto ("Correo o contraseña incorrectos.", code: credenciales_invalidas).
        this.error.set(toUserMessage(err));
      }
      return undefined;
    });
  }
}
