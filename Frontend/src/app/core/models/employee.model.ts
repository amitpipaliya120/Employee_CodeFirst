export interface Employee {
  employeeId: number;
  name: string;
  email: string;
  phone: string;
  department: string;
  designation: string;
  joiningDate: string;
  status: string;
  address?: string;
  createdDate?: string;
  updatedDate?: string;
  documents?: EmployeeDocument[];
}

export interface CreateEmployeeRequest {
  name: string;
  email: string;
  phone: string;
  department: string;
  designation: string;
  joiningDate: string;
  status: string;
  address?: string;
}

export interface UpdateEmployeeRequest extends CreateEmployeeRequest {
  employeeId: number;
}

export interface EmployeeDocument {
  documentId: number;
  employeeId: number;
  originalFileName: string;
  storedFileName: string;
  fileType: string;
  fileSize: number;
  isImage: boolean;
  uploadedDate: string;
}

export interface EmployeeFilterParams {
  searchKeyword?: string;
  department?: string;
  status?: string;
  dateFrom?: string;
  dateTo?: string;
  pageNumber: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ImportSummary {
  fileName: string;
  totalRecords: number;
  successfulRecords: number;
  failedRecords: number;
  errors: ImportError[];
  resultFileToken?: string;
  hasHeaderError?: boolean;
}

export interface ImportError {
  rowNumber: number;
  field: string;
  attemptedValue: string;
  reason: string;
}
