import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { EmployeeDocument } from '../models/employee.model';

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private apiUrl = `${environment.apiUrl}/documents`;

  constructor(private http: HttpClient) {}

  uploadDocuments(employeeId: number, files: File[]): Observable<ApiResponse<EmployeeDocument[]>> {
    const formData = new FormData();
    files.forEach(file => {
      formData.append('files', file, file.name);
    });

    return this.http.post<ApiResponse<EmployeeDocument[]>>(`${this.apiUrl}/upload/${employeeId}`, formData);
  }

  getDocumentsByEmployeeId(employeeId: number): Observable<ApiResponse<EmployeeDocument[]>> {
    return this.http.get<ApiResponse<EmployeeDocument[]>>(`${this.apiUrl}/employee/${employeeId}`);
  }

  downloadDocument(documentId: number, fileName: string): void {
    this.http.get(`${this.apiUrl}/download/${documentId}`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Failed to download document:', err);
      }
    });
  }

  downloadAllZip(employeeId: number): void {
    this.http.get(`${this.apiUrl}/download-all/${employeeId}`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Employee_${employeeId}_Documents.zip`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Failed to download ZIP:', err);
      }
    });
  }

  deleteDocument(documentId: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.apiUrl}/${documentId}`);
  }
}
