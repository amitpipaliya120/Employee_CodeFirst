import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { EmployeeListComponent } from './features/employee/pages/employee-list/employee-list.component';
import { EmployeeFormComponent } from './features/employee/pages/employee-form/employee-form.component';
import { EmployeeDetailComponent } from './features/employee/pages/employee-detail/employee-detail.component';
import { BulkImportComponent } from './features/bulk-import/pages/bulk-import/bulk-import.component';

const routes: Routes = [
  { path: '', redirectTo: 'employees', pathMatch: 'full' },
  { path: 'employees', component: EmployeeListComponent },
  { path: 'employees/new', component: EmployeeFormComponent },
  { path: 'employees/edit/:id', component: EmployeeFormComponent },
  { path: 'employees/:id', component: EmployeeDetailComponent },
  { path: 'import', component: BulkImportComponent },
  { path: '**', redirectTo: 'employees' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
