using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Models;
using Microsoft.Data.SqlClient;

namespace EmployeeManagement.Infrastructure.Services;

public class BulkImportService : IBulkImportService
{
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IDbConnectionRepository _dbConnection;
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRegex = new(@"^[0-9+\-\s()]{7,20}$", RegexOptions.Compiled);

    private static readonly string[] RequiredColumns = new[]
    {
        "Name",
        "Email",
        "Phone",
        "Department",
        "Designation",
        "JoiningDate"
    };

    private static readonly string[] OptionalColumns = new[]
    {
        "Status",
        "Address"
    };

    public BulkImportService(IEmployeeRepository employeeRepo, IDbConnectionRepository dbConnection)
    {
        _employeeRepo = employeeRepo;
        _dbConnection = dbConnection;
    }

    public async Task<ImportSummary> ImportEmployeesAsync(Stream fileStream, string fileName)
    {
        var summary = new ImportSummary
        {
            FileName = fileName,
            TotalRecords = 0,
            SuccessfulRecords = 0,
            FailedRecords = 0,
            Errors = new List<ImportRowError>()
        };

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext != ".csv" && ext != ".xlsx" && ext != ".xls")
        {
            summary.Errors.Add(new ImportRowError
            {
                RowNumber = 0,
                Field = "FileFormat",
                AttemptedValue = ext,
                Reason = "Invalid file format. Only Excel (.xlsx, .xls) and CSV (.csv) files are allowed."
            });
            summary.FailedRecords = 1;
            return summary;
        }

        ParsedFileData parsed;
        try
        {
            if (ext == ".csv")
            {
                parsed = ReadCsvRows(fileStream);
            }
            else
            {
                parsed = ReadExcelRows(fileStream);
            }
        }
        catch (Exception ex)
        {
            summary.Errors.Add(new ImportRowError
            {
                RowNumber = 0,
                Field = "FileParsing",
                AttemptedValue = fileName,
                Reason = $"Error parsing file: {ex.Message}"
            });
            return summary;
        }

        var headerErrors = ValidateHeaders(parsed.Headers);
        if (headerErrors.Count > 0)
        {
            summary.TotalRecords = parsed.Rows.Count;
            summary.SuccessfulRecords = 0;
            summary.FailedRecords = parsed.Rows.Count > 0 ? parsed.Rows.Count : headerErrors.Count;
            summary.HasHeaderError = true;
            summary.Errors.AddRange(headerErrors);
            return summary;
        }

        var rawRows = parsed.Rows;
        summary.TotalRecords = rawRows.Count;
        if (summary.TotalRecords == 0)
        {
            summary.Errors.Add(new ImportRowError
            {
                RowNumber = 0,
                Field = "FileEmpty",
                AttemptedValue = fileName,
                Reason = "The uploaded file does not contain any data rows."
            });
            return summary;
        }

        var candidatesForDatabaseInsert = new List<BulkEmployeeItem>();

        foreach (var item in rawRows)
        {
            int rowNum = item.RowNumber;
            var r = item.Row;
            bool rowHasError = false;

            if (string.IsNullOrWhiteSpace(r.Name))
            {
                summary.Errors.Add(new ImportRowError { RowNumber = rowNum, Field = "Name", AttemptedValue = r.Name ?? "", Reason = "Name is required." });
                rowHasError = true;
            }
            else if (r.Name.Length > 150)
            {
                summary.Errors.Add(new ImportRowError { RowNumber = rowNum, Field = "Name", AttemptedValue = r.Name, Reason = "Name cannot exceed 150 characters." });
                rowHasError = true;
            }

            if (string.IsNullOrWhiteSpace(r.Email))
            {
                summary.Errors.Add(new ImportRowError { RowNumber = rowNum, Field = "Email", AttemptedValue = r.Email ?? "", Reason = "Email is required." });
                rowHasError = true;
            }
            else
            {
                var cleanEmail = r.Email.Trim().ToLowerInvariant();
                if (!EmailRegex.IsMatch(cleanEmail))
                {
                    summary.Errors.Add(new ImportRowError { RowNumber = rowNum, Field = "Email", AttemptedValue = r.Email, Reason = "Invalid email format." });
                    rowHasError = true;
                }
            }

            if (string.IsNullOrWhiteSpace(r.Phone))
            {
                summary.Errors.Add(new ImportRowError { RowNumber = rowNum, Field = "Phone", AttemptedValue = r.Phone ?? "", Reason = "Phone number is required." });
                rowHasError = true;
            }
            else if (!PhoneRegex.IsMatch(r.Phone.Trim()))
            {
                summary.Errors.Add(new ImportRowError { RowNumber = rowNum, Field = "Phone", AttemptedValue = r.Phone, Reason = "Invalid phone number format (7-20)." });
                rowHasError = true;
            }

            if (string.IsNullOrWhiteSpace(r.Department))
            {
                summary.Errors.Add(new ImportRowError { RowNumber = rowNum, Field = "Department", AttemptedValue = r.Department ?? "", Reason = "Department is required." });
                rowHasError = true;
            }

            if (string.IsNullOrWhiteSpace(r.Designation))
            {
                summary.Errors.Add(new ImportRowError { RowNumber = rowNum, Field = "Designation", AttemptedValue = r.Designation ?? "", Reason = "Designation is required." });
                rowHasError = true;
            }

            DateTime joiningDate = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(r.JoiningDate))
            {
                summary.Errors.Add(new ImportRowError { RowNumber = rowNum, Field = "JoiningDate", AttemptedValue = r.JoiningDate ?? "", Reason = "Joining Date is required." });
                rowHasError = true;
            }
            else if (!DateTime.TryParse(r.JoiningDate.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out joiningDate) &&
                     !DateTime.TryParse(r.JoiningDate.Trim(), out joiningDate))
            {
                summary.Errors.Add(new ImportRowError { RowNumber = rowNum, Field = "JoiningDate", AttemptedValue = r.JoiningDate, Reason = "Joining Date must be a valid date (e.g. YYYY-MM-DD or MM/DD/YYYY)." });
                rowHasError = true;
            }

            if (!rowHasError)
            {
                candidatesForDatabaseInsert.Add(new BulkEmployeeItem
                {
                    RowNumber = rowNum,
                    Name = r.Name!.Trim(),
                    Email = r.Email!.Trim().ToLowerInvariant(),
                    Phone = r.Phone!.Trim(),
                    Department = r.Department!.Trim(),
                    Designation = r.Designation!.Trim(),
                    JoiningDate = joiningDate,
                    Status = string.IsNullOrWhiteSpace(r.Status) ? "Active" : r.Status.Trim(),
                    Address = r.Address?.Trim()
                });
            }
            else
            {
                summary.FailedRecords++;
            }
        }

        if (candidatesForDatabaseInsert.Count > 0)
        {
            var bulkResult = await _employeeRepo.BulkInsertEmployeesAsync(candidatesForDatabaseInsert);
            summary.SuccessfulRecords = bulkResult.SuccessfulRecords;
            summary.FailedRecords += bulkResult.FailedRecords;

            if (bulkResult.Errors != null && bulkResult.Errors.Count > 0)
            {
                summary.Errors.AddRange(bulkResult.Errors);
            }
        }

        summary.Errors = summary.Errors.OrderBy(e => e.RowNumber).ToList();

        summary.ResultFileToken = BuildAndSaveResultExcel(parsed, summary, fileName);

        return summary;
    }

    private string BuildAndSaveResultExcel(
        ParsedFileData parsed,
        ImportSummary summary,
        string originalFileName)
    {
        var errorsByRow = summary.Errors
            .GroupBy(e => e.RowNumber)
            .ToDictionary(g => g.Key, g => string.Join("; ", g.Select(e => e.Reason)));

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Import Result");

        var headers = parsed.Headers != null && parsed.Headers.Count > 0
            ? new List<string>(parsed.Headers)
            : new List<string> { "Name", "Email", "Phone", "Department", "Designation", "JoiningDate", "Status", "Address" };

        headers.Add("Status");
        headers.Add("Reason");

        for (int col = 1; col <= headers.Count; col++)
        {
            var cell = ws.Cell(1, col);
            cell.Value = headers[col - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int rowIdx = 2;
        foreach (var (rowNumber, r) in parsed.Rows)
        {
            bool failed = errorsByRow.TryGetValue(rowNumber, out var reason);
            string rowStatus = failed ? "Fail" : "Success";

            for (int col = 1; col <= headers.Count - 2; col++)
            {
                var h = headers[col - 1];
                string val = GetFieldValueByHeader(r, h);
                ws.Cell(rowIdx, col).Value = val;
            }

            int statusCol = headers.Count - 1;
            int reasonCol = headers.Count;

            var statusCell = ws.Cell(rowIdx, statusCol);
            statusCell.Value = rowStatus;

            var reasonCell = ws.Cell(rowIdx, reasonCol);
            reasonCell.Value = failed ? (reason ?? "") : "";

            rowIdx++;
        }

        ws.Columns().AdjustToContents();

        var resultDir = Path.Combine(AppContext.BaseDirectory, "ImportResults");
        Directory.CreateDirectory(resultDir);

        var token = Guid.NewGuid().ToString("N");
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        var filePath = Path.Combine(resultDir, $"{token}_{baseName}_result.xlsx");

        workbook.SaveAs(filePath);
        return token;
    }

    private static string GetFieldValueByHeader(RawEmployeeRow row, string headerName)
    {
        return headerName.Trim().ToLowerInvariant() switch
        {
            "name" => row.Name ?? "",
            "email" => row.Email ?? "",
            "phone" => row.Phone ?? "",
            "department" => row.Department ?? "",
            "designation" => row.Designation ?? "",
            "joiningdate" => row.JoiningDate ?? "",
            "status" => row.Status ?? "",
            "address" => row.Address ?? "",
            _ => ""
        };
    }

    private List<ImportRowError> ValidateHeaders(List<string> fileHeaders)
    {
        var errors = new List<ImportRowError>();
        var headerSet = new HashSet<string>(
            fileHeaders.Where(h => !string.IsNullOrWhiteSpace(h)).Select(h => h.Trim()),
            StringComparer.OrdinalIgnoreCase);

        foreach (var req in RequiredColumns)
        {
            if (!headerSet.Contains(req))
            {
                errors.Add(new ImportRowError
                {
                    RowNumber = 1,
                    Field = req,
                    AttemptedValue = "(missing)",
                    Reason = $"Required column header '{req}' was not found in the file."
                });
            }
        }

        return errors;
    }

    private ParsedFileData ReadCsvRows(Stream stream)
    {
        var result = new ParsedFileData();
        if (stream.CanSeek) stream.Position = 0;

        using (var reader = new StreamReader(stream))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            HeaderValidated = null,
            MissingFieldFound = null
        }))
        {
            csv.Read();
            csv.ReadHeader();

            if (csv.HeaderRecord != null)
            {
                result.Headers = csv.HeaderRecord.Where(h => !string.IsNullOrWhiteSpace(h)).Select(h => h.Trim()).ToList();
            }

            int rowNumber = 1;
            while (csv.Read())
            {
                rowNumber++;
                var row = new RawEmployeeRow
                {
                    Name = GetCsvField(csv, "Name"),
                    Email = GetCsvField(csv, "Email"),
                    Phone = GetCsvField(csv, "Phone"),
                    Department = GetCsvField(csv, "Department"),
                    Designation = GetCsvField(csv, "Designation"),
                    JoiningDate = GetCsvField(csv, "JoiningDate"),
                    Status = GetCsvField(csv, "Status"),
                    Address = GetCsvField(csv, "Address")
                };
                result.Rows.Add((rowNumber, row));
            }
        }
        return result;
    }

    private ParsedFileData ReadExcelRows(Stream stream)
    {
        var result = new ParsedFileData();
        if (stream.CanSeek) stream.Position = 0;

        using (var workbook = new XLWorkbook(stream))
        {
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) return result;

            var firstRow = worksheet.FirstRowUsed();
            if (firstRow == null) return result;

            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in firstRow.CellsUsed())
            {
                var val = cell.GetString().Trim();
                if (!string.IsNullOrEmpty(val))
                {
                    result.Headers.Add(val);
                    if (!headerMap.ContainsKey(val))
                    {
                        headerMap[val] = cell.Address.ColumnNumber;
                    }
                }
            }

            int lastRowNum = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = firstRow.RowNumber() + 1; r <= lastRowNum; r++)
            {
                var row = worksheet.Row(r);
                if (row.IsEmpty()) continue;

                var rawRow = new RawEmployeeRow
                {
                    Name = GetExcelValue(row, headerMap, "Name"),
                    Email = GetExcelValue(row, headerMap, "Email"),
                    Phone = GetExcelValue(row, headerMap, "Phone"),
                    Department = GetExcelValue(row, headerMap, "Department"),
                    Designation = GetExcelValue(row, headerMap, "Designation"),
                    JoiningDate = GetExcelValue(row, headerMap, "JoiningDate"),
                    Status = GetExcelValue(row, headerMap, "Status"),
                    Address = GetExcelValue(row, headerMap, "Address")
                };
                result.Rows.Add((r, rawRow));
            }
        }
        return result;
    }

    private string? GetCsvField(CsvReader csv, params string[] fieldNames)
    {
        foreach (var name in fieldNames)
        {
            if (csv.TryGetField<string>(name, out var val) && !string.IsNullOrWhiteSpace(val))
            {
                return val.Trim();
            }
        }
        return null;
    }

    private string? GetExcelValue(IXLRow row, Dictionary<string, int> headerMap, params string[] fieldNames)
    {
        foreach (var name in fieldNames)
        {
            if (headerMap.TryGetValue(name, out int colIndex))
            {
                var cell = row.Cell(colIndex);
                if (cell.DataType == XLDataType.DateTime)
                {
                    return cell.GetDateTime().ToString("yyyy-MM-dd");
                }
                var str = cell.GetString()?.Trim();
                if (!string.IsNullOrEmpty(str)) return str;
            }
        }
        return null;
    }

    private class ParsedFileData
    {
        public List<string> Headers { get; set; } = new();
        public List<(int RowNumber, RawEmployeeRow Row)> Rows { get; set; } = new();
    }

    private class RawEmployeeRow
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? JoiningDate { get; set; }
        public string? Status { get; set; }
        public string? Address { get; set; }
    }
}
