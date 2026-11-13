namespace EmployeeManagement.Domain.Models;

public class BulkEmployeeItem
{
    public int RowNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public DateTime JoiningDate { get; set; }
    public string? Status { get; set; }
    public string? Address { get; set; }
}

public class BulkInsertResult
{
    public int TotalRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<ImportRowError> Errors { get; set; } = new();
}
