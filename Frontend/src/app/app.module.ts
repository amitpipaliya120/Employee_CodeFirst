import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { SidebarComponent } from './layouts/sidebar/sidebar.component';
import { EmployeeListComponent } from './features/employee/pages/employee-list/employee-list.component';
import { EmployeeFormComponent } from './features/employee/pages/employee-form/employee-form.component';
import { EmployeeDetailComponent } from './features/employee/pages/employee-detail/employee-detail.component';
import { BulkImportComponent } from './features/bulk-import/pages/bulk-import/bulk-import.component';

@NgModule({
  declarations: [
    AppComponent,
    SidebarComponent,
    EmployeeListComponent,
    EmployeeFormComponent,
    EmployeeDetailComponent,
    BulkImportComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    AppRoutingModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
