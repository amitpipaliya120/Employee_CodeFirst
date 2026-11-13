using System.Data;
using Microsoft.Data.SqlClient;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repository;

public class DocumentRepository : IDocumentRepository
{
    private readonly IDbConnectionRepository _dbConnection;

    public DocumentRepository(IDbConnectionRepository dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<ApiResponse<int>> AddDocumentAsync(EmployeeDocument document)
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
                using (var cmd = new SqlCommand("SP_EmployeeDocument_CRUD", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@EmployeeId", document.EmployeeId);
                    cmd.Parameters.AddWithValue("@OriginalFileName", document.OriginalFileName);
                    cmd.Parameters.AddWithValue("@StoredFileName", document.StoredFileName);
                    cmd.Parameters.AddWithValue("@FileType", document.FileType);
                    cmd.Parameters.AddWithValue("@FileSize", document.FileSize);
                    cmd.Parameters.AddWithValue("@IsImage", document.IsImage);
                    cmd.Parameters.AddWithValue("@StoragePath", document.StoragePath);
                    cmd.Parameters.AddWithValue("@Flag", "INSERT");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result.Message = reader["Message"]?.ToString() ?? "";
                            result.Code = reader["Code"]?.ToString() ?? "500";
                            if (result.Code == "200" && reader["DocumentId"] != DBNull.Value)
                            {
                                result.Data = Convert.ToInt32(reader["DocumentId"]);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            HandleDocumentException(result, ex);
        }

        return result;
    }

    public async Task<ApiResponse<List<EmployeeDocument>>> GetDocumentsByEmployeeIdAsync(int employeeId)
    {
        var result = new ApiResponse<List<EmployeeDocument>>
        {
            Code = "500",
            Message = "Internal Server Error",
            Data = new List<EmployeeDocument>()
        };

        try
        {
            using (var con = _dbConnection.CreateConnection())
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand("SP_EmployeeDocument_CRUD", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                    cmd.Parameters.AddWithValue("@Flag", "GETBYEMPLOYEEID");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var docs = new List<EmployeeDocument>();
                        while (await reader.ReadAsync())
                        {
                            docs.Add(MapDocumentFromReader(reader));
                        }

                        string message = "Documents fetched successfully.";
                        string code = "200";
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            message = reader["Message"]?.ToString() ?? message;
                            code = reader["Code"]?.ToString() ?? code;
                        }

                        result.Code = code;
                        result.Message = message;
                        result.Data = docs;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            HandleDocumentException(result, ex);
        }

        return result;
    }

    public async Task<ApiResponse<EmployeeDocument?>> GetDocumentByIdAsync(int documentId)
    {
        var result = new ApiResponse<EmployeeDocument?>
        {
            Code = "404",
            Message = "Document not found",
            Data = null
        };

        try
        {
            using (var con = _dbConnection.CreateConnection())
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand("SP_EmployeeDocument_CRUD", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DocumentId", documentId);
                    cmd.Parameters.AddWithValue("@Flag", "GETBYID");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        EmployeeDocument? doc = null;
                        if (await reader.ReadAsync())
                        {
                            doc = MapDocumentFromReader(reader);
                        }

                        string message = doc != null ? "Document fetched successfully." : "Document not found.";
                        string code = doc != null ? "200" : "404";

                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            message = reader["Message"]?.ToString() ?? message;
                            code = reader["Code"]?.ToString() ?? code;
                        }

                        result.Code = code;
                        result.Message = message;
                        result.Data = doc;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            HandleDocumentException(result, ex);
        }

        return result;
    }

    public async Task<ApiResponse<bool>> DeleteDocumentAsync(int documentId)
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
                using (var cmd = new SqlCommand("SP_EmployeeDocument_CRUD", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DocumentId", documentId);
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
            HandleDocumentException(result, ex);
        }

        return result;
    }

    private static EmployeeDocument MapDocumentFromReader(SqlDataReader reader)
    {
        return new EmployeeDocument
        {
            DocumentId = Convert.ToInt32(reader["DocumentId"]),
            EmployeeId = Convert.ToInt32(reader["EmployeeId"]),
            OriginalFileName = reader["OriginalFileName"]?.ToString() ?? "",
            StoredFileName = reader["StoredFileName"]?.ToString() ?? "",
            FileType = reader["FileType"]?.ToString() ?? "",
            FileSize = Convert.ToInt64(reader["FileSize"]),
            IsImage = Convert.ToBoolean(reader["IsImage"]),
            StoragePath = reader["StoragePath"]?.ToString() ?? "",
            UploadedDate = Convert.ToDateTime(reader["UploadedDate"])
        };
    }

    private static void HandleDocumentException<T>(ApiResponse<T> result, Exception ex)
    {
        if (ex is SqlException || ex is TimeoutException ||
            ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("handshake", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase))
        {
            result.Code = "503";
            result.Message = "Unable to connect to the database server. Please try again.";
            return;
        }

        result.Code = "500";
        result.Message = "An unexpected error occurred while processing documents. Please try again.";
    }
}
