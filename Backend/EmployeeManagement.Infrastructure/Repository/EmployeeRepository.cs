using System.Data;
using Microsoft.Data.SqlClient;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Models;

namespace EmployeeManagement.Infrastructure.Repository;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDbConnectionRepository _dbConnection;

    public EmployeeRepository(IDbConnectionRepository dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<ApiResponse<PagedResult<Employee>>> GetEmployeesPagedAsync(EmployeeFilterParams filterParams)
    {
        var result = new ApiResponse<PagedResult<Employee>>
        {
            Code = "500",
            Message = "Internal Server Error",
            Data = new PagedResult<Employee>()
        };

        try
        {
            using (var con = _dbConnection.CreateConnection())
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand("SP_Employee_CRUD", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@SearchKeyword", (object?)filterParams.SearchKeyword ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Department", (object?)filterParams.Department ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Status", (object?)filterParams.Status ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateFrom", (object?)filterParams.DateFrom ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateTo", (object?)filterParams.DateTo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PageNumber", filterParams.PageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", filterParams.PageSize);
                    cmd.Parameters.AddWithValue("@Flag", "GETPAGED_FILTERED");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int totalCount = 0;
                        if (await reader.ReadAsync())
                        {
                            totalCount = Convert.ToInt32(reader["TotalCount"]);
                        }

                        var employees = new List<Employee>();
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                employees.Add(MapEmployeeFromReader(reader));
                            }
                        }

                        string message = "Employees fetched successfully.";
                        string code = "200";
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            message = reader["Message"]?.ToString() ?? message;
                            code = reader["Code"]?.ToString() ?? code;
                        }

                        result.Code = code;
                        result.Message = message;
                        result.Data = new PagedResult<Employee>
                        {
                            Items = employees,
                            TotalCount = totalCount,
                            PageNumber = filterParams.PageNumber,
                            PageSize = filterParams.PageSize
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            HandleRepositoryException(result, ex);
        }

        return result;
    }

    public async Task<ApiResponse<List<Employee>>> GetAllEmployeesAsync()
    {
        var result = new ApiResponse<List<Employee>>
        {
            Code = "500",
            Message = "Internal Server Error",
            Data = new List<Employee>()
        };

        try
        {
            using (var con = _dbConnection.CreateConnection())
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand("SP_Employee_CRUD", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Flag", "GETALL");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var employees = new List<Employee>();
                        while (await reader.ReadAsync())
                        {
                            employees.Add(MapEmployeeFromReader(reader));
                        }

                        string message = "Employees fetched successfully.";
                        string code = "200";
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            message = reader["Message"]?.ToString() ?? message;
                            code = reader["Code"]?.ToString() ?? code;
                        }

                        result.Code = code;
                        result.Message = message;
                        result.Data = employees;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            HandleRepositoryException(result, ex);
        }

        return result;
    }

    public async Task<ApiResponse<Employee?>> GetEmployeeByIdAsync(int employeeId)
    {
        var result = new ApiResponse<Employee?>
        {
            Code = "404",
            Message = "Employee not found",
            Data = null
        };

        try
        {
            using (var con = _dbConnection.CreateConnection())
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand("SP_Employee_CRUD", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                    cmd.Parameters.AddWithValue("@Flag", "GETBYID");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        Employee? emp = null;
                        if (await reader.ReadAsync())
                        {
                            emp = MapEmployeeFromReader(reader);
                        }

                        string message = emp != null ? "Employee fetched successfully." : "Employee not found.";
                        string code = emp != null ? "200" : "404";

                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            message = reader["Message"]?.ToString() ?? message;
                            code = reader["Code"]?.ToString() ?? code;
                        }

                        result.Code = code;
                        result.Message = message;
                        result.Data = emp;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            HandleRepositoryException(result, ex);
        }

        return result;
    }

    public async Task<ApiResponse<int>> CreateEmployeeAsync(Employee employee)
    {
        var result = new ApiResponse<int>
        {
            Code = "500",
            Message = "Internal Server Error",
            Data = 0
        };

        try
        {
            using (var con = _dbConnection.CreateConnection())
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand("SP_Employee_CRUD", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Name", employee.Name);
                    cmd.Parameters.AddWithValue("@Email", employee.Email);
                    cmd.Parameters.AddWithValue("@Phone", employee.Phone);
                    cmd.Parameters.AddWithValue("@Department", employee.Department);
                    cmd.Parameters.AddWithValue("@Designation", employee.Designation);
                    cmd.Parameters.AddWithValue("@JoiningDate", employee.JoiningDate);
                    cmd.Parameters.AddWithValue("@Status", (object?)employee.Status ?? "Active");
                    cmd.Parameters.AddWithValue("@Address", (object?)employee.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Flag", "INSERT");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result.Message = reader["Message"]?.ToString() ?? "";
                            result.Code = reader["Code"]?.ToString() ?? "500";
                            if (result.Code == "200" && reader["EmployeeId"] != DBNull.Value)
                            {
                                result.Data = Convert.ToInt32(reader["EmployeeId"]);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            HandleRepositoryException(result, ex);
        }

        return result;
    }

    public async Task<ApiResponse<bool>> UpdateEmployeeAsync(Employee employee)
    {
        var result = new ApiResponse<bool>
        {
            Code = "500",
            Message = "Internal Server Error",
            Data = false
        };

        try
        {
            using (var con = _dbConnection.CreateConnection())
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand("SP_Employee_CRUD", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@EmployeeId", employee.EmployeeId);
                    cmd.Parameters.AddWithValue("@Name", employee.Name);
                    cmd.Parameters.AddWithValue("@Email", employee.Email);
                    cmd.Parameters.AddWithValue("@Phone", employee.Phone);
                    cmd.Parameters.AddWithValue("@Department", employee.Department);
                    cmd.Parameters.AddWithValue("@Designation", employee.Designation);
                    cmd.Parameters.AddWithValue("@JoiningDate", employee.JoiningDate);
                    cmd.Parameters.AddWithValue("@Status", (object?)employee.Status ?? "Active");
                    cmd.Parameters.AddWithValue("@Address", (object?)employee.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Flag", "UPDATE");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result.Message = reader["Message"]?.ToString() ?? "";
                            result.Code = reader["Code"]?.ToString() ?? "500";
                            result.Data = result.Code == "200";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            HandleRepositoryException(result, ex);
        }

        return result;
    }

    public async Task<ApiResponse<bool>> DeleteEmployeeAsync(int employeeId)
    {
        var result = new ApiResponse<bool>
        {
            Code = "500",
            Message = "Internal Server Error",
            Data = false
        };

        try
        {
            using (var con = _dbConnection.CreateConnection())
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand("SP_Employee_CRUD", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                    cmd.Parameters.AddWithValue("@Flag", "DELETE");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result.Message = reader["Message"]?.ToString() ?? "";
                            result.Code = reader["Code"]?.ToString() ?? "500";
                            result.Data = result.Code == "200";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            HandleRepositoryException(result, ex);
        }

        return result;
    }

    private static Employee MapEmployeeFromReader(SqlDataReader reader)
    {
        return new Employee
        {
            EmployeeId = Convert.ToInt32(reader["EmployeeId"]),
            Name = reader["Name"]?.ToString() ?? "",
            Email = reader["Email"]?.ToString() ?? "",
            Phone = reader["Phone"]?.ToString() ?? "",
            Department = reader["Department"]?.ToString() ?? "",
            Designation = reader["Designation"]?.ToString() ?? "",
            JoiningDate = Convert.ToDateTime(reader["JoiningDate"]),
            Status = reader["Status"]?.ToString() ?? "Active",
            Address = reader["Address"] != DBNull.Value ? reader["Address"]?.ToString() : null,
            CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : DateTime.UtcNow,
            UpdatedDate = reader["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedDate"]) : null,
            IsDeleted = reader["IsDeleted"] != DBNull.Value && Convert.ToBoolean(reader["IsDeleted"])
        };
    }

    public async Task<BulkInsertResult> BulkInsertEmployeesAsync(IEnumerable<BulkEmployeeItem> employees)
    {
        var result = new BulkInsertResult();
        var dt = CreateEmployeeDataTable();

        foreach (var item in employees)
        {
            dt.Rows.Add(
                item.RowNumber,
                item.Name,
                item.Email,
                item.Phone,
                item.Department,
                item.Designation,
                item.JoiningDate,
                (object?)item.Status ?? DBNull.Value,
                (object?)item.Address ?? DBNull.Value
            );
        }

        try
        {
            using (var con = _dbConnection.CreateConnection())
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand("SP_Employee_BulkInsert", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 180;

                    var param = cmd.Parameters.AddWithValue("@Employees", dt);
                    param.SqlDbType = SqlDbType.Structured;
                    param.TypeName = "dbo.EmployeeTableType";

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result.TotalRecords = reader["TotalRecords"] != DBNull.Value ? Convert.ToInt32(reader["TotalRecords"]) : 0;
                            result.SuccessfulRecords = reader["SuccessfulRecords"] != DBNull.Value ? Convert.ToInt32(reader["SuccessfulRecords"]) : 0;
                            result.FailedRecords = reader["FailedRecords"] != DBNull.Value ? Convert.ToInt32(reader["FailedRecords"]) : 0;
                        }

                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Errors.Add(new ImportRowError
                                {
                                    RowNumber = reader["RowNumber"] != DBNull.Value ? Convert.ToInt32(reader["RowNumber"]) : 0,
                                    Field = reader["Field"]?.ToString() ?? "",
                                    AttemptedValue = reader["AttemptedValue"]?.ToString() ?? "",
                                    Reason = reader["Reason"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.FailedRecords = dt.Rows.Count;
            string reason = (ex is SqlException || ex is TimeoutException || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("handshake", StringComparison.OrdinalIgnoreCase))
                ? "Unable to connect to the database. Please verify that SQL Server is running and try again."
                : "An unexpected database error occurred during bulk import. Please try again.";

            result.Errors.Add(new ImportRowError
            {
                RowNumber = 0,
                Field = "DatabaseError",
                AttemptedValue = "(empty)",
                Reason = reason
            });
        }

        return result;
    }

    private static void HandleRepositoryException<T>(ApiResponse<T> result, Exception ex)
    {
        if (ex is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627) ||
            ex.Message.Contains("UQ_tblEmployees_Email", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
        {
            result.Code = "400";
            result.Message = "An employee with this email address already exists.";
            return;
        }

        if (ex is SqlException || ex is TimeoutException ||
            ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("handshake", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase))
        {
            result.Code = "503";
            result.Message = "Unable to connect to the database. Please verify that SQL Server is running and try again.";
            return;
        }

        result.Code = "500";
        result.Message = "An unexpected error occurred while processing your request. Please try again.";
    }

    private static DataTable CreateEmployeeDataTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("RowNumber", typeof(int));
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Email", typeof(string));
        dt.Columns.Add("Phone", typeof(string));
        dt.Columns.Add("Department", typeof(string));
        dt.Columns.Add("Designation", typeof(string));
        dt.Columns.Add("JoiningDate", typeof(DateTime));
        dt.Columns.Add("Status", typeof(string));
        dt.Columns.Add("Address", typeof(string));
        return dt;
    }
}
