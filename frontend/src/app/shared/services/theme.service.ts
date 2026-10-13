import { effect, Injectable, signal } from '@angular/core';

type Theme = 'light' | 'dark';

/** Tema claro/oscuro con signals. Se guarda en localStorage y se aplica con la clase .dark en <html>. */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<Theme>(localStorage.getItem('theme') === 'dark' ? 'dark' : 'light');

  constructor() {
    effect(() => {
      const theme = this.theme();
      localStorage.setItem('theme', theme);
      document.documentElement.classList.toggle('dark', theme === 'dark');
      document.documentElement.setAttribute('data-color-scheme', theme);
    });
  }

  toggleTheme(): void {
    this.theme.update((t) => (t === 'light' ? 'dark' : 'light'));
  }
}
