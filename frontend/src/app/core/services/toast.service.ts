import { Injectable, signal } from '@angular/core';

import { toUserMessage } from '../utils/api-error';

export type ToastType = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

/** Avisos flotantes del panel (éxito / error). */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly items = signal<Toast[]>([]);
  readonly toasts = this.items.asReadonly();
  private nextId = 1;

  show(message: string, type: ToastType = 'info', ms = 4000): void {
    const id = this.nextId++;
    this.items.update((list) => [...list, { id, type, message }]);
    setTimeout(() => this.dismiss(id), ms);
  }

  success(message: string): void {
    this.show(message, 'success');
  }

  /** Acepta un texto o cualquier error HTTP (se convierte en un mensaje comprensible). */
  error(error: unknown): void {
    this.show(typeof error === 'string' ? error : toUserMessage(error), 'error', 6000);
  }

  dismiss(id: number): void {
    this.items.update((list) => list.filter((t) => t.id !== id));
  }
}
