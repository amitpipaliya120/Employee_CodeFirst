import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { EmployeeService } from 'src/app/core/services/employee.service';
import { DocumentService } from 'src/app/core/services/document.service';
import { NotificationService } from 'src/app/core/services/notification.service';
import { EmployeeDocument } from 'src/app/core/models/employee.model';

@Component({
  selector: 'app-employee-form',
  templateUrl: './employee-form.component.html',
  styleUrls: ['./employee-form.component.css']
})
export class EmployeeFormComponent implements OnInit {
  form!: FormGroup;
  isEditMode = false;
  employeeId = 0;
  submitting = false;
  loadingEmployee = false;

  selectedFiles: File[] = [];
  existingDocuments: EmployeeDocument[] = [];

  selectedDocForDelete: EmployeeDocument | null = null;
  deletingDoc = false;

  allowedExtensions = ['.jpg', '.jpeg', '.png', '.pdf', '.docx', '.xlsx', '.csv'];
  maxFileSizeBytes = 10 * 1024 * 1024;

  departments = [
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
  statuses = ['Active', 'Inactive'];

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private employeeService: EmployeeService,
    private documentService: DocumentService,
    private notify: NotificationService
  ) {}

  ngOnInit(): void {
    this.initForm();
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEditMode = true;
      this.employeeId = parseInt(idParam, 10);
      this.loadEmployeeData();
    }
  }

  initForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      phone: ['', [Validators.required, Validators.pattern(/^[0-9+\-\s()]{7,20}$/)]],
      department: ['', Validators.required],
      designation: ['', [Validators.required, Validators.maxLength(100)]],
      joiningDate: ['', Validators.required],
      status: ['Active', Validators.required],
      address: ['', Validators.maxLength(500)]
    });
  }

  loadEmployeeData(): void {
    this.loadingEmployee = true;
    this.employeeService.getEmployeeById(this.employeeId).subscribe({
      next: (res) => {
        this.loadingEmployee = false;
        if (res.code === '200' && res.data) {
          const emp = res.data;
          this.form.patchValue({
            name: emp.name,
            email: emp.email,
            phone: emp.phone,
            department: emp.department,
            designation: emp.designation,
            joiningDate: emp.joiningDate ? emp.joiningDate.split('T')[0] : '',
            status: emp.status,
            address: emp.address || ''
          });
          this.existingDocuments = emp.documents || [];
        } else {
          this.notify.showError(res.message || 'Failed to load employee.');
          this.router.navigate(['/employees']);
        }
      },
      error: () => {
        this.loadingEmployee = false;
        this.notify.showError('Error loading employee details.');
      }
    });
  }

  onFilesSelected(event: any): void {
    const files: FileList = event.target.files;
    this.processFiles(files);
  }

  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer && event.dataTransfer.files) {
      this.processFiles(event.dataTransfer.files);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  processFiles(fileList: FileList): void {
    for (let i = 0; i < fileList.length; i++) {
      const file = fileList[i];
      const ext = '.' + file.name.split('.').pop()?.toLowerCase();

      if (!this.allowedExtensions.includes(ext)) {
        this.notify.showError(`File "${file.name}" has an unsupported format. Allowed: ${this.allowedExtensions.join(', ')}`);
        continue;
      }

      if (file.size > this.maxFileSizeBytes) {
        this.notify.showError(`File "${file.name}" exceeds the 10MB size limit.`);
        continue;
      }

      if (!this.selectedFiles.some(f => f.name === file.name && f.size === file.size)) {
        this.selectedFiles.push(file);
      }
    }
  }

  removeSelectedFile(index: number): void {
    this.selectedFiles.splice(index, 1);
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  isImageFile(file: File): boolean {
    return file.type.startsWith('image/');
  }

  requestDeleteDocument(doc: EmployeeDocument): void {
    this.selectedDocForDelete = doc;
  }

  cancelDeleteDocument(): void {
    if (this.deletingDoc) return;
    this.selectedDocForDelete = null;
  }

  confirmDeleteDocument(): void {
    if (!this.selectedDocForDelete) return;

    const doc = this.selectedDocForDelete;
    this.deletingDoc = true;
    this.documentService.deleteDocument(doc.documentId).subscribe({
      next: (res) => {
        this.deletingDoc = false;
        if (res.code === '200') {
          this.notify.showSuccess(`Document '${doc.originalFileName}' deleted successfully.`);
          this.existingDocuments = this.existingDocuments.filter(d => d.documentId !== doc.documentId);
          this.selectedDocForDelete = null;
        } else {
          this.notify.showError(res.message || 'Failed to delete document.');
        }
      },
      error: () => {
        this.deletingDoc = false;
        this.notify.showError('Error deleting document.');
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notify.showWarning('Please fill out all required fields properly.');
      return;
    }

    this.submitting = true;
    const formValue = this.form.value;

    if (this.isEditMode) {
      const updateData = {
        ...formValue,
        employeeId: this.employeeId
      };

      this.employeeService.updateEmployee(this.employeeId, updateData).subscribe({
        next: (res) => {
          if (res.code === '200') {
            this.uploadFilesAndFinish(this.employeeId, 'Employee updated successfully!');
          } else {
            this.handleFormError({ error: res }, 'Failed to update employee.');
          }
        },
        error: (err) => {
          this.handleFormError(err, 'An error occurred while updating the employee.');
        }
      });
    } else {
      this.employeeService.createEmployee(formValue).subscribe({
        next: (res) => {
          if (res.code === '200' && res.data) {
            const newId = res.data;
            this.uploadFilesAndFinish(newId, 'Employee created successfully!');
          } else {
            this.handleFormError({ error: res }, 'Failed to create employee.');
          }
        },
        error: (err) => {
          this.handleFormError(err, 'An error occurred while creating the employee.');
        }
      });
    }
  }

  private handleFormError(err: any, defaultFallback: string): void {
    this.submitting = false;
    let message = defaultFallback;

    if (err?.error?.message) {
      message = err.error.message;
    } else if (err?.error?.Message) {
      message = err.error.Message;
    } else if (typeof err?.error === 'string') {
      message = err.error;
    } else if (err?.message) {
      message = err.message;
    }

    const lowerMsg = message.toLowerCase();
    if (
      lowerMsg.includes('email') &&
      (lowerMsg.includes('exist') || lowerMsg.includes('taken') || lowerMsg.includes('duplicate') || lowerMsg.includes('unique'))
    ) {
      message = 'An employee with this email address already exists. Please use a different email.';
      this.form.get('email')?.setErrors({ alreadyExists: true });
      this.form.get('email')?.markAsTouched();
    } else if (
      lowerMsg.includes('handshake') ||
      lowerMsg.includes('timeout') ||
      lowerMsg.includes('pre-login') ||
      lowerMsg.includes('sql') ||
      lowerMsg.includes('unable to respond')
    ) {
      message = 'Database connection timed out. Please try again.';
    }

    this.notify.showError(message);
  }

  private uploadFilesAndFinish(empId: number, successMessage: string): void {
    if (this.selectedFiles.length > 0) {
      this.documentService.uploadDocuments(empId, this.selectedFiles).subscribe({
        next: (docRes) => {
          this.submitting = false;
          this.notify.showSuccess(`${successMessage}`);
          this.router.navigate(['/employees']);
        },
        error: (err) => {
          this.submitting = false;
          this.notify.showWarning(`${successMessage} However, some documents failed to upload.`);
          this.router.navigate(['/employees']);
        }
      });
    } else {
      this.submitting = false;
      this.notify.showSuccess(successMessage);
      this.router.navigate(['/employees']);
    }
  }
}
