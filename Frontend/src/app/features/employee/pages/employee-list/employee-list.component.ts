import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Employee, EmployeeFilterParams } from 'src/app/core/models/employee.model';
import { EmployeeService } from 'src/app/core/services/employee.service';
import { NotificationService } from 'src/app/core/services/notification.service';

@Component({
  selector: 'app-employee-list',
  templateUrl: './employee-list.component.html',
  styleUrls: ['./employee-list.component.css']
})
export class EmployeeListComponent implements OnInit, OnDestroy {
  employees: Employee[] = [];
  loading = false;
  totalCount = 0;
  totalPages = 0;

  private searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;

  filterParams: EmployeeFilterParams = {
    searchKeyword: '',
    department: '',
    status: '',
    dateFrom: '',
    dateTo: '',
    pageNumber: 1,
    pageSize: 10
  };

  departments: string[] = [
    'Engineering',
    'Human Resources',
    'Finance',
    'Marketing',
    'Operations',
    'Sales',
    'Product',
    'Customer Support',
    'Information Technology'
  ];

  statuses: string[] = ['Active', 'Inactive'];

  selectedEmployeeForDelete: Employee | null = null;
  deleting = false;

  constructor(
    private employeeService: EmployeeService,
    private notify: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged()
    ).subscribe(() => {
      this.onSearch();
    });

    this.loadEmployees();
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  onSearchInputChange(value: string): void {
    this.searchSubject.next(value || '');
  }

  clearSearch(): void {
    this.filterParams.searchKeyword = '';
    this.onSearch();
  }

  loadEmployees(): void {
    this.loading = true;
    this.employeeService.getEmployees(this.filterParams).subscribe({
      next: (res) => {
        this.loading = false;
        if (res && res.code === '200' && res.data) {
          this.employees = res.data.items || [];
          this.totalCount = res.data.totalCount || 0;
          this.totalPages = res.data.totalPages || 0;
        } else {
          this.notify.showError(res.message || 'Failed to load employees');
        }
      },
      error: (err) => {
        this.loading = false;
        this.notify.showError('Unable to connect to server. Please ensure the backend API is running.');
      }
    });
  }

  onSearch(): void {
    this.filterParams.pageNumber = 1;
    this.loadEmployees();
  }

  resetFilters(): void {
    this.filterParams = {
      searchKeyword: '',
      department: '',
      status: '',
      dateFrom: '',
      dateTo: '',
      pageNumber: 1,
      pageSize: this.filterParams.pageSize
    };
    this.loadEmployees();
  }

  onPageChange(newPage: number): void {
    if (newPage >= 1 && newPage <= this.totalPages) {
      this.filterParams.pageNumber = newPage;
      this.loadEmployees();
    }
  }

  onPageSizeChange(event: any): void {
    this.filterParams.pageSize = parseInt(event.target.value, 10);
    this.filterParams.pageNumber = 1;
    this.loadEmployees();
  }

  viewDetails(id: number): void {
    this.router.navigate(['/employees', id]);
  }

  editEmployee(id: number): void {
    this.router.navigate(['/employees/edit', id]);
  }

  confirmDelete(emp: Employee): void {
    this.selectedEmployeeForDelete = emp;
  }

  cancelDelete(): void {
    this.selectedEmployeeForDelete = null;
  }

  deleteConfirmed(): void {
    if (!this.selectedEmployeeForDelete) return;

    this.deleting = true;
    this.employeeService.deleteEmployee(this.selectedEmployeeForDelete.employeeId).subscribe({
      next: (res) => {
        this.deleting = false;
        if (res.code === '200') {
          this.notify.showSuccess(`Employee '${this.selectedEmployeeForDelete?.name}' deleted successfully.`);
          this.selectedEmployeeForDelete = null;
          this.loadEmployees();
        } else {
          this.notify.showError(res.message || 'Failed to delete employee.');
        }
      },
      error: () => {
        this.deleting = false;
        this.notify.showError('An error occurred while deleting the employee.');
      }
    });
  }
}
