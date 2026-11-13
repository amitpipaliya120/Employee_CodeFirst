import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface ToastMessage {
  id: number;
  type: 'success' | 'danger' | 'warning' | 'info';
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private toasts$ = new BehaviorSubject<ToastMessage[]>([]);
  public toasts = this.toasts$.asObservable();
  private counter = 0;

  showSuccess(message: string): void {
    this.addToast('success', message);
  }

  showError(message: string): void {
    const sanitized = this.sanitizeErrorMessage(message);
    this.addToast('danger', sanitized);
  }

  private sanitizeErrorMessage(message: string): string {
    if (!message) {
      return 'An unexpected error occurred. Please try again.';
    }

    const lower = message.toLowerCase();

    if (
      lower.includes('handshake') ||
      lower.includes('pre-login') ||
      lower.includes('timeout') ||
      lower.includes('sqlexception') ||
      lower.includes('sql server') ||
      lower.includes('tcp provider') ||
      lower.includes('network-related') ||
      lower.includes('named pipes') ||
      lower.includes('server was unable to respond') ||
      lower.includes('connection reset') ||
      lower.includes('http failure response')
    ) {
      return 'Unable to connect to the database server. Please try again.';
    }

    return message;
  }

  showWarning(message: string): void {
    this.addToast('warning', message);
  }

  showInfo(message: string): void {
    this.addToast('info', message);
  }

  private addToast(type: 'success' | 'danger' | 'warning' | 'info', message: string): void {
    const id = ++this.counter;
    const current = this.toasts$.value;
    this.toasts$.next([...current, { id, type, message }]);

    setTimeout(() => {
      this.removeToast(id);
    }, 4500);
  }

  removeToast(id: number): void {
    const updated = this.toasts$.value.filter(t => t.id !== id);
    this.toasts$.next(updated);
  }
}
