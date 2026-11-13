import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ImportSummary } from '../models/employee.model';

@Injectable({
  providedIn: 'root'
})
export class ImportService {
  private apiUrl = `${environment.apiUrl}/import`;

  constructor(private http: HttpClient) {}

  uploadImportFile(file: File): Observable<ImportSummary> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ImportSummary>(`${this.apiUrl}/upload`, formData);
  }

  downloadResult(token: string, originalFileName: string): void {
    this.http.get(`${this.apiUrl}/download-result/${token}`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const baseName = originalFileName.replace(/\.[^/.]+$/, '');
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${baseName}_result.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => console.error('Failed to download result file.')
    });
  }

  downloadSampleCsv(): void {
    this.http.get(`${this.apiUrl}/sample-csv`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'sample_employees.csv';
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => console.error('Failed to download sample CSV:', err)
    });
  }

  downloadSampleExcel(): void {
    this.http.get(`${this.apiUrl}/sample-excel`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'sample_employees.xlsx';
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => console.error('Failed to download sample Excel:', err)
    });
  }
}
