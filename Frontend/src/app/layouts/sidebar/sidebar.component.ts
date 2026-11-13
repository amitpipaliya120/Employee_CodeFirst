import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { SidebarService } from '../../core/services/sidebar.service';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
})
export class SidebarComponent implements OnInit, OnDestroy {
  isCollapsed: boolean = false;
  isMobileOpen: boolean = false;
  private subs = new Subscription();

  constructor(public sidebarService: SidebarService) {}

  ngOnInit(): void {
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

  toggleCollapse(): void {
    this.sidebarService.toggleCollapse();
  }

  closeMobile(): void {
    this.sidebarService.closeMobile();
  }
}
