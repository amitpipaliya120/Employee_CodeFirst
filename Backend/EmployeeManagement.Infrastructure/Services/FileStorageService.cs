using Microsoft.Extensions.Configuration;
using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _uploadDirectory;

    public FileStorageService(IConfiguration configuration)
    {
        var configuredPath = configuration["FileStorage:UploadPath"];
        _uploadDirectory = !string.IsNullOrWhiteSpace(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, "Uploads");

        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }
    }

    public async Task<(string storedFileName, string fullPath)> SaveFileAsync(Stream fileStream, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_uploadDirectory, uniqueFileName);

        using (var outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }
            await fileStream.CopyToAsync(outputStream);
        }

        return (uniqueFileName, fullPath);
    }

    public Task<Stream> GetFileStreamAsync(string storagePath)
    {
        if (!File.Exists(storagePath))
        {
            throw new FileNotFoundException("Requested file was not found on the server.", storagePath);
        }

        Stream stream = new FileStream(storagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public bool DeleteFile(string storagePath)
    {
        try
        {
            if (File.Exists(storagePath))
            {
                File.Delete(storagePath);
                return true;
            }
        }
        catch
        {
        }
        return false;
    }
}
