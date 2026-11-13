namespace EmployeeManagement.Application.Interfaces;

public interface IFileStorageService
{
    Task<(string storedFileName, string fullPath)> SaveFileAsync(Stream fileStream, string originalFileName);
    Task<Stream> GetFileStreamAsync(string storagePath);
    bool DeleteFile(string storagePath);
}
