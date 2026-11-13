using EmployeeManagement.Domain;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Models;

namespace EmployeeManagement.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<ApiResponse<PagedResult<Employee>>> GetEmployeesPagedAsync(EmployeeFilterParams filterParams);
    Task<ApiResponse<List<Employee>>> GetAllEmployeesAsync();
    Task<ApiResponse<Employee?>> GetEmployeeByIdAsync(int employeeId);
    Task<ApiResponse<int>> CreateEmployeeAsync(Employee employee);
    Task<ApiResponse<bool>> UpdateEmployeeAsync(Employee employee);
    Task<ApiResponse<bool>> DeleteEmployeeAsync(int employeeId);
    Task<BulkInsertResult> BulkInsertEmployeesAsync(IEnumerable<BulkEmployeeItem> employees);
}
