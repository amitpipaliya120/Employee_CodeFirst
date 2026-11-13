using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Infrastructure.Repository;

public class DbConnectionRepository : IDbConnectionRepository
{
    private readonly string _connectionString;

    public DbConnectionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
