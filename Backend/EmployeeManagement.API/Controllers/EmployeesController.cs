using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain;
using EmployeeManagement.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] EmployeeFilterParams filterParams)
    {
        var result = await _employeeService.GetEmployeesPagedAsync(filterParams);
        return StatusCode(int.TryParse(result.Code, out int code) ? code : 200, result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _employeeService.GetEmployeeByIdAsync(id);
        return StatusCode(int.TryParse(result.Code, out int code) ? code : 200, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        var result = await _employeeService.CreateEmployeeAsync(dto);
        return StatusCode(int.TryParse(result.Code, out int code) ? code : 200, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeDto dto)
    {
        dto.EmployeeId = id;
        var result = await _employeeService.UpdateEmployeeAsync(dto);
        return StatusCode(int.TryParse(result.Code, out int code) ? code : 200, result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _employeeService.DeleteEmployeeAsync(id);
        return StatusCode(int.TryParse(result.Code, out int code) ? code : 200, result);
    }
}
