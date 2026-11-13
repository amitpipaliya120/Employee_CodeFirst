import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { Employee, CreateEmployeeRequest, UpdateEmployeeRequest, EmployeeFilterParams, PagedResult } from '../models/employee.model';

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private apiUrl = `${environment.apiUrl}/employees`;

  constructor(private http: HttpClient) {}

  getEmployees(params: EmployeeFilterParams): Observable<ApiResponse<PagedResult<Employee>>> {
    let httpParams = new HttpParams()
      .set('pageNumber', params.pageNumber.toString())
      .set('pageSize', params.pageSize.toString());

    if (params.searchKeyword && params.searchKeyword.trim() !== '') {
      httpParams = httpParams.set('searchKeyword', params.searchKeyword.trim());
    }
    if (params.department && params.department.trim() !== '') {
      httpParams = httpParams.set('department', params.department.trim());
    }
    if (params.status && params.status.trim() !== '') {
      httpParams = httpParams.set('status', params.status.trim());
    }
    if (params.dateFrom) {
      httpParams = httpParams.set('dateFrom', params.dateFrom);
    }
    if (params.dateTo) {
      httpParams = httpParams.set('dateTo', params.dateTo);
    }

    return this.http.get<ApiResponse<PagedResult<Employee>>>(this.apiUrl, { params: httpParams });
  }

  getEmployeeById(id: number): Observable<ApiResponse<Employee>> {
    return this.http.get<ApiResponse<Employee>>(`${this.apiUrl}/${id}`);
  }

  createEmployee(employee: CreateEmployeeRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.apiUrl, employee);
  }

  updateEmployee(id: number, employee: UpdateEmployeeRequest): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.apiUrl}/${id}`, employee);
  }

  deleteEmployee(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.apiUrl}/${id}`);
  }
}
