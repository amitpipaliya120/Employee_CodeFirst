using Microsoft.Data.SqlClient;
namespace EmployeeManagement.Application.Interfaces;

public interface IDbConnectionRepository
{
    SqlConnection CreateConnection();
}
