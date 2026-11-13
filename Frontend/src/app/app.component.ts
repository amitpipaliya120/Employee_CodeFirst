import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { NotificationService, ToastMessage } from './core/services/notification.service';
import { SidebarService } from './core/services/sidebar.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'EmpManagement';
  toasts: ToastMessage[] = [];
  isCollapsed = false;
  isMobileOpen = false;
  private subs = new Subscription();

  constructor(
    public notify: NotificationService,
    public sidebarService: SidebarService
  ) {}

  ngOnInit(): void {
    this.subs.add(
      this.notify.toasts.subscribe((t: ToastMessage[]) => {
        this.toasts = t;
      })
    );
    this.subs.add(
      this.sidebarService.isCollapsed$.subscribe((collapsed: boolean) => {
        this.isCollapsed = collapsed;
      })
    );
    this.subs.add(
      this.sidebarService.isMobileOpen$.subscribe((open: boolean) => {
        this.isMobileOpen = open;
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  toggleSidebar(): void {
    this.sidebarService.toggle();
  }

  closeMobileSidebar(): void {
    this.sidebarService.closeMobile();
  }

  closeToast(id: number): void {
    this.notify.removeToast(id);
  }
}
