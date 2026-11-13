using EmployeeManagement.Domain.Models;

namespace EmployeeManagement.Application.Interfaces;

public interface IBulkImportService
{
    Task<ImportSummary> ImportEmployeesAsync(Stream fileStream, string fileName);
}
