import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { ImportService } from 'src/app/core/services/import.service';
import { NotificationService } from 'src/app/core/services/notification.service';
import { ImportSummary } from 'src/app/core/models/employee.model';

@Component({
  selector: 'app-bulk-import',
  templateUrl: './bulk-import.component.html',
  styleUrls: ['./bulk-import.component.css']
})
export class BulkImportComponent {
  selectedFile: File | null = null;
  importing = false;
  summary: ImportSummary | null = null;
  dragOver = false;

  constructor(
    private importService: ImportService,
    private notify: NotificationService,
    private router: Router
  ) { }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    this.validateAndSetFile(file);
  }

  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = false;
    if (event.dataTransfer && event.dataTransfer.files.length > 0) {
      this.validateAndSetFile(event.dataTransfer.files[0]);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = false;
  }

  validateAndSetFile(file: File): void {
    if (!file) return;

    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    if (ext !== '.csv' && ext !== '.xlsx' && ext !== '.xls') {
      this.notify.showError('Invalid file format. Please upload a CSV (.csv) or Excel (.xlsx) file.');
      return;
    }

    this.selectedFile = file;
    this.summary = null;
  }

  removeFile(): void {
    this.selectedFile = null;
    this.summary = null;
  }

  startImport(): void {
    if (!this.selectedFile) {
      this.notify.showWarning('Please select an Excel or CSV file first.');
      return;
    }

    this.importing = true;
    this.summary = null;

    this.importService.uploadImportFile(this.selectedFile).subscribe({
      next: (result) => {
        this.importing = false;
        this.summary = result;

        if (result.resultFileToken) {
          this.importService.downloadResult(result.resultFileToken, result.fileName || 'import_result.xlsx');
        }

        if (result.successfulRecords > 0 && result.failedRecords === 0) {
          this.notify.showSuccess(`Successfully imported all ${result.successfulRecords} employees! Status file downloaded.`);
        } else if (result.successfulRecords > 0 && result.failedRecords > 0) {
          this.notify.showWarning(`Imported ${result.successfulRecords} record(s). ${result.failedRecords} record(s) failed validation. Status file downloaded.`);
        } else {
          this.notify.showError(`Import failed. All ${result.failedRecords} record(s) had validation errors. Status file downloaded.`);
        }
      },
      error: (err) => {
        this.importing = false;
        this.notify.showError('An error occurred during bulk import processing.');
      }
    });
  }

  downloadSampleCsv(): void {
    this.importService.downloadSampleCsv();
  }

  downloadSampleExcel(): void {
    this.importService.downloadSampleExcel();
  }

  downloadResultFile(): void {
    if (this.summary?.resultFileToken) {
      this.importService.downloadResult(this.summary.resultFileToken, this.summary.fileName || 'import_result.xlsx');
    }
  }

  goToEmployeeList(): void {
    this.router.navigate(['/employees']);
  }
}
