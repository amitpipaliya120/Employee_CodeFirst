import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Employee, EmployeeDocument } from 'src/app/core/models/employee.model';
import { EmployeeService } from 'src/app/core/services/employee.service';
import { DocumentService } from 'src/app/core/services/document.service';
import { NotificationService } from 'src/app/core/services/notification.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-employee-detail',
  templateUrl: './employee-detail.component.html',
  styleUrls: ['./employee-detail.component.css']
})
export class EmployeeDetailComponent implements OnInit {
  employeeId = 0;
  employee: Employee | null = null;
  documents: EmployeeDocument[] = [];
  loading = true;
  uploadingDocs = false;
  apiBaseUrl = environment.apiUrl;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private employeeService: EmployeeService,
    private documentService: DocumentService,
    private notify: NotificationService
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.employeeId = parseInt(idParam, 10);
      this.loadEmployee();
    }
  }

  loadEmployee(): void {
    this.loading = true;
    this.employeeService.getEmployeeById(this.employeeId).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.code === '200' && res.data) {
          this.employee = res.data;
          this.documents = res.data.documents || [];
        } else {
          this.notify.showError(res.message || 'Failed to load employee.');
          this.router.navigate(['/employees']);
        }
      },
      error: () => {
        this.loading = false;
        this.notify.showError('Error connecting to backend API.');
      }
    });
  }

  downloadSingle(doc: EmployeeDocument): void {
    this.documentService.downloadDocument(doc.documentId, doc.originalFileName);
  }

  downloadAllZip(): void {
    if (this.documents.length === 0) {
      this.notify.showWarning('No documents available to download.');
      return;
    }
    this.notify.showInfo('Preparing ZIP download...');
    this.documentService.downloadAllZip(this.employeeId);
  }

  deleteDocument(doc: EmployeeDocument): void {
    if (confirm(`Are you sure you want to delete file "${doc.originalFileName}"?`)) {
      this.documentService.deleteDocument(doc.documentId).subscribe({
        next: (res) => {
          if (res.code === '200') {
            this.notify.showSuccess('Document deleted.');
            this.documents = this.documents.filter(d => d.documentId !== doc.documentId);
          } else {
            this.notify.showError(res.message || 'Failed to delete document.');
          }
        },
        error: () => this.notify.showError('Error deleting document.')
      });
    }
  }

  onMoreFilesSelected(event: any): void {
    const files: FileList = event.target.files;
    if (!files || files.length === 0) return;

    const fileArray: File[] = [];
    for (let i = 0; i < files.length; i++) {
      fileArray.push(files[i]);
    }

    this.uploadingDocs = true;
    this.documentService.uploadDocuments(this.employeeId, fileArray).subscribe({
      next: (res) => {
        this.uploadingDocs = false;
        if (res.code === '200' && res.data) {
          this.notify.showSuccess('Document(s) uploaded successfully.');
          this.loadEmployee();
        } else {
          this.notify.showError(res.message || 'Failed to upload documents.');
        }
      },
      error: () => {
        this.uploadingDocs = false;
        this.notify.showError('Error uploading documents.');
      }
    });
  }

  formatFileSize(bytes: number): string {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  getDownloadUrl(docId: number): string {
    return `${this.apiBaseUrl}/documents/download/${docId}`;
  }
}
