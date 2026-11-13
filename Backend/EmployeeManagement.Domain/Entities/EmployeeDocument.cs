namespace EmployeeManagement.Domain.Entities;

public class EmployeeDocument
{
    public int DocumentId { get; set; }
    public int EmployeeId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public bool IsImage { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
}
