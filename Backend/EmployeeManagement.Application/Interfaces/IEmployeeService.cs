using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Domain;
using EmployeeManagement.Domain.Models;

namespace EmployeeManagement.Application.Interfaces;

public interface IEmployeeService
{
    Task<ApiResponse<PagedResult<EmployeeDto>>> GetEmployeesPagedAsync(EmployeeFilterParams filterParams);
    Task<ApiResponse<EmployeeDto?>> GetEmployeeByIdAsync(int employeeId);
    Task<ApiResponse<int>> CreateEmployeeAsync(CreateEmployeeDto dto);
    Task<ApiResponse<bool>> UpdateEmployeeAsync(UpdateEmployeeDto dto);
    Task<ApiResponse<bool>> DeleteEmployeeAsync(int employeeId);
}
