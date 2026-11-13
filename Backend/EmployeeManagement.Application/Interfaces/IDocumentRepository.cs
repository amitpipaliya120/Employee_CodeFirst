using EmployeeManagement.Domain;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Interfaces;

public interface IDocumentRepository
{
    Task<ApiResponse<int>> AddDocumentAsync(EmployeeDocument document);
    Task<ApiResponse<List<EmployeeDocument>>> GetDocumentsByEmployeeIdAsync(int employeeId);
    Task<ApiResponse<EmployeeDocument?>> GetDocumentByIdAsync(int documentId);
    Task<ApiResponse<bool>> DeleteDocumentAsync(int documentId);
}
