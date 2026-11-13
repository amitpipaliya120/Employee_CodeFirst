namespace EmployeeManagement.Domain.Models;

public class ImportSummary
{
    public string FileName { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<ImportRowError> Errors { get; set; } = new();
    public string? ResultFileToken { get; set; }
    public bool HasHeaderError { get; set; }
}

public class ImportRowError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string AttemptedValue { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
