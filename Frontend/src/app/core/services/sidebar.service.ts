import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class SidebarService {
  private collapsedSubject = new BehaviorSubject<boolean>(false);
  public isCollapsed$ = this.collapsedSubject.asObservable();

  private mobileOpenSubject = new BehaviorSubject<boolean>(false);
  public isMobileOpen$ = this.mobileOpenSubject.asObservable();

  constructor(private router: Router) {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.closeMobile();
    });
  }

  get isCollapsed(): boolean {
    return this.collapsedSubject.value;
  }

  get isMobileOpen(): boolean {
    return this.mobileOpenSubject.value;
  }

  toggle(): void {
    if (window.innerWidth < 992) {
      this.mobileOpenSubject.next(!this.mobileOpenSubject.value);
    } else {
      this.collapsedSubject.next(!this.collapsedSubject.value);
    }
  }

  toggleCollapse(): void {
    this.collapsedSubject.next(!this.collapsedSubject.value);
  }

  toggleMobile(): void {
    this.mobileOpenSubject.next(!this.mobileOpenSubject.value);
  }

  closeMobile(): void {
    if (this.mobileOpenSubject.value) {
      this.mobileOpenSubject.next(false);
    }
  }

  openMobile(): void {
    this.mobileOpenSubject.next(true);
  }
}
