using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Domain;

namespace EmployeeManagement.Application.Interfaces;

public interface IDocumentService
{
    Task<ApiResponse<EmployeeDocumentDto>> UploadDocumentAsync(int employeeId, Stream fileStream, string originalFileName, string contentType, long fileSize);
    Task<ApiResponse<List<EmployeeDocumentDto>>> GetDocumentsByEmployeeIdAsync(int employeeId);
    Task<(Stream stream, string contentType, string fileName)?> DownloadDocumentAsync(int documentId);
    Task<(Stream zipStream, string zipFileName)?> DownloadAllDocumentsZipAsync(int employeeId);
    Task<ApiResponse<bool>> DeleteDocumentAsync(int documentId);
}
