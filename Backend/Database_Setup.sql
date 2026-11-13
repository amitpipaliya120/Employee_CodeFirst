
CREATE DATABASE EmployeeManagementDB;

USE EmployeeManagementDB;
GO

BEGIN
    CREATE TABLE dbo.tblEmployees (
        EmployeeId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        Email NVARCHAR(255) NOT NULL UNIQUE,
        Phone VARCHAR(20) NOT NULL,
        Department NVARCHAR(100) NOT NULL,
        Designation NVARCHAR(100) NOT NULL,
        JoiningDate DATETIME NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Active',
        Address NVARCHAR(500) NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedDate DATETIME NULL,
        IsDeleted BIT NOT NULL DEFAULT 0
    );

    CREATE NONCLUSTERED INDEX IX_tblEmployees_Department ON dbo.tblEmployees(Department);
    CREATE NONCLUSTERED INDEX IX_tblEmployees_Status ON dbo.tblEmployees(Status);
    CREATE NONCLUSTERED INDEX IX_tblEmployees_JoiningDate ON dbo.tblEmployees(JoiningDate);
END
GO

BEGIN
    CREATE TABLE dbo.tblEmployeeDocuments (
        DocumentId INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId INT NOT NULL FOREIGN KEY REFERENCES dbo.tblEmployees(EmployeeId) ON DELETE CASCADE,
        OriginalFileName NVARCHAR(255) NOT NULL,
        StoredFileName NVARCHAR(255) NOT NULL,
        FileType NVARCHAR(100) NOT NULL,
        FileSize BIGINT NOT NULL,
        IsImage BIT NOT NULL DEFAULT 0,
        StoragePath NVARCHAR(500) NOT NULL,
        UploadedDate DATETIME NOT NULL DEFAULT GETDATE()
    );

    CREATE NONCLUSTERED INDEX IX_tblEmployeeDocuments_EmployeeId ON dbo.tblEmployeeDocuments(EmployeeId);
END
GO

BEGIN
    CREATE TYPE dbo.EmployeeTableType AS TABLE (
        RowNumber INT NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Email NVARCHAR(255) NOT NULL,
        Phone VARCHAR(20) NOT NULL,
        Department NVARCHAR(100) NOT NULL,
        Designation NVARCHAR(100) NOT NULL,
        JoiningDate DATETIME NOT NULL,
        Status NVARCHAR(50) NULL,
        Address NVARCHAR(500) NULL
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_Employee_CRUD
    @EmployeeId      INT             = NULL,
    @Name            NVARCHAR(150)   = NULL,
    @Email           NVARCHAR(255)   = NULL,
    @Phone           VARCHAR(20)     = NULL,
    @Department      NVARCHAR(100)   = NULL,
    @Designation     NVARCHAR(100)   = NULL,
    @JoiningDate     DATETIME        = NULL,
    @Status          NVARCHAR(50)    = NULL,
    @Address         NVARCHAR(500)   = NULL,
    @SearchKeyword   NVARCHAR(100)   = NULL,
    @DateFrom        DATETIME        = NULL,
    @DateTo          DATETIME        = NULL,
    @PageNumber      INT             = 1,
    @PageSize        INT             = 10,
    @Flag            NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @Flag = 'INSERT'
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.tblEmployees WHERE Email = LTRIM(RTRIM(@Email)) AND IsDeleted = 0)
            BEGIN
                SELECT 'An employee with this email address already exists.' AS Message, 400 AS Code, 0 AS EmployeeId;
                RETURN;
            END

            INSERT INTO dbo.tblEmployees (Name, Email, Phone, Department, Designation, JoiningDate, Status, Address, CreatedDate, IsDeleted)
            VALUES (@Name, @Email, @Phone, @Department, @Designation, @JoiningDate, ISNULL(@Status, 'Active'), @Address, GETDATE(), 0);

            SELECT 'Employee inserted successfully.' AS Message, 200 AS Code, SCOPE_IDENTITY() AS EmployeeId;
        END

        ELSE IF @Flag = 'UPDATE'
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.tblEmployees WHERE EmployeeId = @EmployeeId AND IsDeleted = 0)
            BEGIN
                SELECT 'Employee not found for update.' AS Message, 404 AS Code, 0 AS EmployeeId;
                RETURN;
            END

            IF EXISTS (SELECT 1 FROM dbo.tblEmployees WHERE Email = LTRIM(RTRIM(@Email)) AND EmployeeId <> @EmployeeId AND IsDeleted = 0)
            BEGIN
                SELECT 'Email is already taken by another employee.' AS Message, 400 AS Code, 0 AS EmployeeId;
                RETURN;
            END

            UPDATE dbo.tblEmployees
            SET Name        = ISNULL(@Name, Name),
                Email       = ISNULL(@Email, Email),
                Phone       = ISNULL(@Phone, Phone),
                Department  = ISNULL(@Department, Department),
                Designation = ISNULL(@Designation, Designation),
                JoiningDate = ISNULL(@JoiningDate, JoiningDate),
                Status      = ISNULL(@Status, Status),
                Address     = ISNULL(@Address, Address),
                UpdatedDate = GETDATE()
            WHERE EmployeeId = @EmployeeId;

            SELECT 'Employee updated successfully.' AS Message, 200 AS Code, @EmployeeId AS EmployeeId;
        END

        ELSE IF @Flag = 'DELETE'
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.tblEmployees WHERE EmployeeId = @EmployeeId AND IsDeleted = 0)
            BEGIN
                UPDATE dbo.tblEmployees
                SET IsDeleted = 1, UpdatedDate = GETDATE()
                WHERE EmployeeId = @EmployeeId;

                SELECT 'Employee deleted successfully.' AS Message, 200 AS Code;
            END
            ELSE
            BEGIN
                SELECT 'Employee not found for delete.' AS Message, 404 AS Code;
            END
        END

        ELSE IF @Flag = 'GETBYID'
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.tblEmployees WHERE EmployeeId = @EmployeeId AND IsDeleted = 0)
            BEGIN
                SELECT TOP 1 * FROM dbo.tblEmployees WHERE EmployeeId = @EmployeeId AND IsDeleted = 0;
                SELECT 'Employee fetched successfully.' AS Message, 200 AS Code;
            END
            ELSE
            BEGIN
                SELECT 'Employee not found.' AS Message, 404 AS Code;
            END
        END

        ELSE IF @Flag = 'GETALL'
        BEGIN
            SELECT * FROM dbo.tblEmployees WHERE IsDeleted = 0 ORDER BY EmployeeId DESC;
            SELECT 'Employee list fetched successfully.' AS Message, 200 AS Code;
        END

        ELSE IF @Flag = 'GETPAGED_FILTERED'
        BEGIN
            DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

            SELECT COUNT(1) AS TotalCount
            FROM dbo.tblEmployees
            WHERE IsDeleted = 0
              AND (@SearchKeyword IS NULL OR LTRIM(RTRIM(@SearchKeyword)) = '' 
                   OR Name LIKE '%' + @SearchKeyword + '%' 
                   OR Email LIKE '%' + @SearchKeyword + '%' 
                   OR Phone LIKE '%' + @SearchKeyword + '%')
              AND (@Department IS NULL OR LTRIM(RTRIM(@Department)) = '' OR Department = @Department)
              AND (@Status IS NULL OR LTRIM(RTRIM(@Status)) = '' OR Status = @Status)
              AND (@DateFrom IS NULL OR JoiningDate >= @DateFrom)
              AND (@DateTo IS NULL OR JoiningDate <= @DateTo)
            OPTION (RECOMPILE);

            SELECT EmployeeId, Name, Email, Phone, Department, Designation, JoiningDate, Status, Address, CreatedDate, UpdatedDate, IsDeleted
            FROM dbo.tblEmployees
            WHERE IsDeleted = 0
              AND (@SearchKeyword IS NULL OR LTRIM(RTRIM(@SearchKeyword)) = '' 
                   OR Name LIKE '%' + @SearchKeyword + '%' 
                   OR Email LIKE '%' + @SearchKeyword + '%' 
                   OR Phone LIKE '%' + @SearchKeyword + '%')
              AND (@Department IS NULL OR LTRIM(RTRIM(@Department)) = '' OR Department = @Department)
              AND (@Status IS NULL OR LTRIM(RTRIM(@Status)) = '' OR Status = @Status)
              AND (@DateFrom IS NULL OR JoiningDate >= @DateFrom)
              AND (@DateTo IS NULL OR JoiningDate <= @DateTo)
            ORDER BY EmployeeId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            OPTION (RECOMPILE);

            SELECT 'Filtered employee list fetched successfully.' AS Message, 200 AS Code;
        END

        ELSE
        BEGIN
            SELECT 'Invalid flag specified.' AS Message, 400 AS Code;
        END
    END TRY
    BEGIN CATCH
        SELECT ERROR_MESSAGE() AS Message, 500 AS Code;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_EmployeeDocument_CRUD
    @DocumentId        INT             = NULL,
    @EmployeeId        INT             = NULL,
    @OriginalFileName  NVARCHAR(255)   = NULL,
    @StoredFileName    NVARCHAR(255)   = NULL,
    @FileType          NVARCHAR(100)   = NULL,
    @FileSize          BIGINT          = NULL,
    @IsImage           BIT             = 0,
    @StoragePath       NVARCHAR(500)   = NULL,
    @Flag              NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @Flag = 'INSERT'
        BEGIN
            INSERT INTO dbo.tblEmployeeDocuments (EmployeeId, OriginalFileName, StoredFileName, FileType, FileSize, IsImage, StoragePath, UploadedDate)
            VALUES (@EmployeeId, @OriginalFileName, @StoredFileName, @FileType, @FileSize, @IsImage, @StoragePath, GETDATE());

            SELECT 'Document uploaded successfully.' AS Message, 200 AS Code, SCOPE_IDENTITY() AS DocumentId;
        END

        ELSE IF @Flag = 'GETBYEMPLOYEEID'
        BEGIN
            SELECT * FROM dbo.tblEmployeeDocuments WHERE EmployeeId = @EmployeeId ORDER BY DocumentId DESC;
            SELECT 'Documents fetched successfully.' AS Message, 200 AS Code;
        END

        ELSE IF @Flag = 'GETBYID'
        BEGIN
            SELECT TOP 1 * FROM dbo.tblEmployeeDocuments WHERE DocumentId = @DocumentId;
            SELECT 'Document fetched successfully.' AS Message, 200 AS Code;
        END

        ELSE IF @Flag = 'DELETE'
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.tblEmployeeDocuments WHERE DocumentId = @DocumentId)
            BEGIN
                DELETE FROM dbo.tblEmployeeDocuments WHERE DocumentId = @DocumentId;
                SELECT 'Document deleted successfully.' AS Message, 200 AS Code;
            END
            ELSE
            BEGIN
                SELECT 'Document not found.' AS Message, 404 AS Code;
            END
        END

        ELSE
        BEGIN
            SELECT 'Invalid flag specified.' AS Message, 400 AS Code;
        END
    END TRY
    BEGIN CATCH
        SELECT ERROR_MESSAGE() AS Message, 500 AS Code;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Employee_BulkInsert
    @Employees dbo.EmployeeTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #RejectedRows (
        RowNumber      INT,
        Field          NVARCHAR(50),
        AttemptedValue NVARCHAR(255),
        Reason         NVARCHAR(500)
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        ;WITH RankedIncoming AS (
            SELECT RowNumber, Email,
                   ROW_NUMBER() OVER (PARTITION BY LOWER(LTRIM(RTRIM(Email))) ORDER BY RowNumber) AS Occurrence
            FROM @Employees
        )
        INSERT INTO #RejectedRows (RowNumber, Field, AttemptedValue, Reason)
        SELECT RowNumber, 'Email', Email, 'Duplicate email within uploaded file'
        FROM RankedIncoming
        WHERE Occurrence > 1;

        INSERT INTO #RejectedRows (RowNumber, Field, AttemptedValue, Reason)
        SELECT e.RowNumber, 'Email', e.Email,
               CASE 
                   WHEN db.IsDeleted = 1 THEN 'Email belongs to deleted employee and cannot be reused'
                   ELSE 'Email already exists in system'
               END
        FROM @Employees e
        INNER JOIN dbo.tblEmployees db ON LOWER(LTRIM(RTRIM(db.Email))) = LOWER(LTRIM(RTRIM(e.Email)))
        WHERE e.RowNumber NOT IN (SELECT RowNumber FROM #RejectedRows);

        INSERT INTO dbo.tblEmployees (Name, Email, Phone, Department, Designation, JoiningDate, Status, Address, CreatedDate, IsDeleted)
        SELECT 
            LTRIM(RTRIM(e.Name)),
            LOWER(LTRIM(RTRIM(e.Email))),
            LTRIM(RTRIM(e.Phone)),
            LTRIM(RTRIM(e.Department)),
            LTRIM(RTRIM(e.Designation)),
            e.JoiningDate,
            ISNULL(NULLIF(LTRIM(RTRIM(e.Status)), ''), 'Active'),
            NULLIF(LTRIM(RTRIM(e.Address)), ''),
            GETDATE(), 
            0
        FROM @Employees e
        WHERE e.RowNumber NOT IN (SELECT RowNumber FROM #RejectedRows);

        DECLARE @InsertedCount INT = @@ROWCOUNT;
        DECLARE @RejectedCount INT = (SELECT COUNT(1) FROM #RejectedRows);
        DECLARE @TotalCount    INT = (SELECT COUNT(1) FROM @Employees);

        COMMIT TRANSACTION;

        SELECT @TotalCount AS TotalRecords, @InsertedCount AS SuccessfulRecords, @RejectedCount AS FailedRecords;

        SELECT RowNumber, Field, AttemptedValue, Reason
        FROM #RejectedRows
        ORDER BY RowNumber ASC;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

        SELECT (SELECT COUNT(1) FROM @Employees) AS TotalRecords,
               0 AS SuccessfulRecords,
               (SELECT COUNT(1) FROM @Employees) AS FailedRecords;

        SELECT 0 AS RowNumber, 'DatabaseError' AS Field, '' AS AttemptedValue, ERROR_MESSAGE() AS Reason;
    END CATCH

    IF OBJECT_ID('tempdb..#RejectedRows') IS NOT NULL
        DROP TABLE #RejectedRows;
END;
GO