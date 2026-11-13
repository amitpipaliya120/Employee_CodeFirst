Employee Management System
==========================

1. Tech Stack
-------------
* Backend: ASP.NET Core 8 Web API, ADO.NET (Microsoft.Data.SqlClient)
* Frontend: Angular 17, TypeScript, HTML5, CSS3
* Database: Microsoft SQL Server


2. Project Structure
--------------------
```
Backend/
├── EmployeeManagement.API/
├── EmployeeManagement.Application/
├── EmployeeManagement.Domain/
├── EmployeeManagement.Infrastructure/
└── Database_Setup.sql

Frontend/
└── src/app/
    ├── core/
    └── features/
        ├── employee/
        └── bulk-import/
```


3. Features
-----------
* Employee Management: CRUD operations, server-side pagination, search by name/email/phone, and filtering by department, status, and joining date range.
* Document Handling: Upload, view, download, and delete attachments (PDF, DOCX, XLSX, CSV, JPG, PNG) with automatic image compression for images over 1MB.
* Bulk Import: Upload employees via Excel (.xlsx) or CSV (.csv) using SQL Server Table-Valued Parameter with duplicate email checks and automatic Excel status report download.


4. Application Ports
--------------------
* Frontend (Angular): http://localhost:4200
* Backend (.NET API): https://localhost:44331
