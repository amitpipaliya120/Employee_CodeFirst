using System.Text;
using ClosedXML.Excel;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly IBulkImportService _bulkImportService;

    public ImportController(IBulkImportService bulkImportService)
    {
        _bulkImportService = bulkImportService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ImportSummary
            {
                FileName = string.Empty,
                TotalRecords = 0,
                SuccessfulRecords = 0,
                FailedRecords = 1,
                Errors = new List<ImportRowError>
                {
                    new() { RowNumber = 0, Field = "File", AttemptedValue = "", Reason = "Please select a valid CSV or Excel file to upload." }
                }
            });
        }

        using var stream = file.OpenReadStream();
        var summary = await _bulkImportService.ImportEmployeesAsync(stream, file.FileName);
        return Ok(summary);
    }

    [HttpGet("download-result/{token}")]
    public IActionResult DownloadResult(string token)
    {
        var resultDir = Path.Combine(AppContext.BaseDirectory, "ImportResults");
        if (!Directory.Exists(resultDir))
            return NotFound("No result file found.");

        var matchingFile = Directory.GetFiles(resultDir, $"{token}_*_result.xlsx").FirstOrDefault();
        if (matchingFile == null)
            return NotFound("Result file not found or already expired.");

        var bytes = System.IO.File.ReadAllBytes(matchingFile);
        var downloadName = Path.GetFileName(matchingFile).Substring(token.Length + 1);

        System.IO.File.Delete(matchingFile);

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", downloadName);
    }

    [HttpGet("sample-csv")]
    public IActionResult DownloadSampleCsv()
    {
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("Name,Email,Phone,Department,Designation,JoiningDate,Status,Address");
        csvBuilder.AppendLine("Alice Johnson,alice.johnson@example.com,9876543220,Engineering,Software Engineer,2024-01-10,Active,101 Code Street");
        csvBuilder.AppendLine("David Miller,david.miller@example.com,9876543221,Human Resources,HR Executive,2023-09-15,Active,202 People Ave");
        csvBuilder.AppendLine("Invalid name,invalid email,123,Marketing,Specialist,invalid-date,Active,303 Growth Rd");

        var bytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
        return File(bytes, "text/csv", "sample_employees.csv");
    }

    [HttpGet("sample-excel")]
    public IActionResult DownloadSampleExcel()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Employees");

        string[] headers = { "Name", "Email", "Phone", "Department", "Designation", "JoiningDate", "Status", "Address" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        worksheet.Cell(2, 1).Value = "Alice Johnson";
        worksheet.Cell(2, 2).Value = "alice.johnson@example.com";
        worksheet.Cell(2, 3).Value = "9876543220";
        worksheet.Cell(2, 4).Value = "Engineering";
        worksheet.Cell(2, 5).Value = "Software Engineer";
        worksheet.Cell(2, 6).Value = "2024-01-10";
        worksheet.Cell(2, 7).Value = "Active";
        worksheet.Cell(2, 8).Value = "101 Code Street";

        worksheet.Cell(3, 1).Value = "David Miller";
        worksheet.Cell(3, 2).Value = "david.miller@example.com";
        worksheet.Cell(3, 3).Value = "9876543221";
        worksheet.Cell(3, 4).Value = "Human Resources";
        worksheet.Cell(3, 5).Value = "HR Executive";
        worksheet.Cell(3, 6).Value = "2023-09-15";
        worksheet.Cell(3, 7).Value = "Active";
        worksheet.Cell(3, 8).Value = "202 People Ave";

        worksheet.Cell(4, 1).Value = "Invalid name";
        worksheet.Cell(4, 2).Value = "invalid email";
        worksheet.Cell(4, 3).Value = "123";
        worksheet.Cell(4, 4).Value = "Marketing";
        worksheet.Cell(4, 5).Value = "Specialist";
        worksheet.Cell(4, 6).Value = "invalid";
        worksheet.Cell(4, 7).Value = "Active";
        worksheet.Cell(4, 8).Value = "303 Growth Rd";

        worksheet.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "sample_employees.xlsx");
    }
}
