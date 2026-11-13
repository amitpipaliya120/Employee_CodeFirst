namespace EmployeeManagement.Application.Interfaces;

public interface IImageCompressionService
{
    bool IsImage(string fileName, string contentType);
    Task<MemoryStream> CompressImageIfNeededAsync(Stream inputStream, string contentType, long maxSizeBytes = 1048576);
}
