using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Validators;
using EmployeeManagement.Domain;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Models;

namespace EmployeeManagement.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IDocumentRepository _documentRepo;

    public EmployeeService(IEmployeeRepository employeeRepo, IDocumentRepository documentRepo)
    {
        _employeeRepo = employeeRepo;
        _documentRepo = documentRepo;
    }

    public async Task<ApiResponse<PagedResult<EmployeeDto>>> GetEmployeesPagedAsync(EmployeeFilterParams filterParams)
    {
        var repoResult = await _employeeRepo.GetEmployeesPagedAsync(filterParams);
        if (repoResult.Code != "200" || repoResult.Data == null)
        {
            return new ApiResponse<PagedResult<EmployeeDto>>
            {
                Code = repoResult.Code,
                Message = repoResult.Message,
                Data = new PagedResult<EmployeeDto>()
            };
        }

        var dtoItems = repoResult.Data.Items.Select(MapToDto).ToList();

        var pagedDto = new PagedResult<EmployeeDto>
        {
            Items = dtoItems,
            TotalCount = repoResult.Data.TotalCount,
            PageNumber = repoResult.Data.PageNumber,
            PageSize = repoResult.Data.PageSize
        };

        return new ApiResponse<PagedResult<EmployeeDto>>
        {
            Code = "200",
            Message = "Employees retrieved successfully.",
            Data = pagedDto
        };
    }

    public async Task<ApiResponse<EmployeeDto?>> GetEmployeeByIdAsync(int employeeId)
    {
        var repoResult = await _employeeRepo.GetEmployeeByIdAsync(employeeId);
        if (repoResult.Code != "200" || repoResult.Data == null)
        {
            return new ApiResponse<EmployeeDto?>
            {
                Code = repoResult.Code,
                Message = repoResult.Message,
                Data = null
            };
        }

        var dto = MapToDto(repoResult.Data);

        var docsResult = await _documentRepo.GetDocumentsByEmployeeIdAsync(employeeId);
        if (docsResult.Code == "200" && docsResult.Data != null)
        {
            dto.Documents = docsResult.Data.Select(d => new EmployeeDocumentDto
            {
                DocumentId = d.DocumentId,
                EmployeeId = d.EmployeeId,
                OriginalFileName = d.OriginalFileName,
                StoredFileName = d.StoredFileName,
                FileType = d.FileType,
                FileSize = d.FileSize,
                IsImage = d.IsImage,
                UploadedDate = d.UploadedDate
            }).ToList();
        }

        return new ApiResponse<EmployeeDto?>
        {
            Code = "200",
            Message = "Employee retrieved successfully.",
            Data = dto
        };
    }

    public async Task<ApiResponse<int>> CreateEmployeeAsync(CreateEmployeeDto dto)
    {
        var validator = new CreateEmployeeValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var firstError = validation.Errors.First().ErrorMessage;
            return new ApiResponse<int>
            {
                Code = "400",
                Message = firstError,
                Data = 0
            };
        }

        var entity = new Employee
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Phone = dto.Phone.Trim(),
            Department = dto.Department.Trim(),
            Designation = dto.Designation.Trim(),
            JoiningDate = dto.JoiningDate,
            Status = dto.Status.Trim(),
            Address = dto.Address?.Trim()
        };

        return await _employeeRepo.CreateEmployeeAsync(entity);
    }

    public async Task<ApiResponse<bool>> UpdateEmployeeAsync(UpdateEmployeeDto dto)
    {
        var validator = new UpdateEmployeeValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var firstError = validation.Errors.First().ErrorMessage;
            return new ApiResponse<bool>
            {
                Code = "400",
                Message = firstError,
                Data = false
            };
        }

        var entity = new Employee
        {
            EmployeeId = dto.EmployeeId,
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Phone = dto.Phone.Trim(),
            Department = dto.Department.Trim(),
            Designation = dto.Designation.Trim(),
            JoiningDate = dto.JoiningDate,
            Status = dto.Status.Trim(),
            Address = dto.Address?.Trim()
        };

        return await _employeeRepo.UpdateEmployeeAsync(entity);
    }

    public async Task<ApiResponse<bool>> DeleteEmployeeAsync(int employeeId)
    {
        return await _employeeRepo.DeleteEmployeeAsync(employeeId);
    }

    private static EmployeeDto MapToDto(Employee e)
    {
        return new EmployeeDto
        {
            EmployeeId = e.EmployeeId,
            Name = e.Name,
            Email = e.Email,
            Phone = e.Phone,
            Department = e.Department,
            Designation = e.Designation,
            JoiningDate = e.JoiningDate,
            Status = e.Status,
            Address = e.Address,
            CreatedDate = e.CreatedDate,
            UpdatedDate = e.UpdatedDate
        };
    }
}
