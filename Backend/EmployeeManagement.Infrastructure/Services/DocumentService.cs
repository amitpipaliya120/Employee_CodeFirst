using System.IO.Compression;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain;
using EmployeeManagement.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace EmployeeManagement.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepo;
    private readonly IFileStorageService _fileStorage;
    private readonly IImageCompressionService _imageCompression;
    private readonly HashSet<string> _allowedExtensions;
    private readonly long _maxFileSizeBytes;

    public DocumentService(
        IDocumentRepository documentRepo,
        IFileStorageService fileStorage,
        IImageCompressionService imageCompression,
        IConfiguration configuration)
    {
        _documentRepo = documentRepo;
        _fileStorage = fileStorage;
        _imageCompression = imageCompression;

        var configuredExts = configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>();
        _allowedExtensions = configuredExts != null && configuredExts.Length > 0
            ? new HashSet<string>(configuredExts, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".xlsx", ".csv" };

        var configuredMaxBytes = configuration.GetValue<long?>("FileStorage:MaxFileSizeBytes");
        _maxFileSizeBytes = configuredMaxBytes ?? (10 * 1024 * 1024);
    }

    public async Task<ApiResponse<EmployeeDocumentDto>> UploadDocumentAsync(
        int employeeId,
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileSize)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();

        if (!_allowedExtensions.Contains(ext))
        {
            return new ApiResponse<EmployeeDocumentDto>
            {
                Code = "400",
                Message = $"File type '{ext}' is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}",
                Data = null
            };
        }

        if (fileSize > _maxFileSizeBytes)
        {
            return new ApiResponse<EmployeeDocumentDto>
            {
                Code = "400",
                Message = $"File size ({fileSize / (1024 * 1024.0):F2} MB) exceeds maximum allowed size ({_maxFileSizeBytes / (1024 * 1024.0):F2} MB).",
                Data = null
            };
        }

        bool isImg = _imageCompression.IsImage(originalFileName, contentType);
        Stream streamToSave = fileStream;
        long finalFileSize = fileSize;

        if (isImg && fileSize > 1048576)
        {
            var compressedStream = await _imageCompression.CompressImageIfNeededAsync(fileStream, contentType, 1048576);
            streamToSave = compressedStream;
            finalFileSize = compressedStream.Length;
        }

        var (storedFileName, fullPath) = await _fileStorage.SaveFileAsync(streamToSave, originalFileName);

        var docEntity = new EmployeeDocument
        {
            EmployeeId = employeeId,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            FileType = ext,
            FileSize = finalFileSize,
            IsImage = isImg,
            StoragePath = fullPath,
            UploadedDate = DateTime.UtcNow
        };

        var repoResult = await _documentRepo.AddDocumentAsync(docEntity);
        if (repoResult.Code != "200")
        {
            _fileStorage.DeleteFile(fullPath);
            return new ApiResponse<EmployeeDocumentDto>
            {
                Code = repoResult.Code,
                Message = repoResult.Message,
                Data = null
            };
        }

        var dto = new EmployeeDocumentDto
        {
            DocumentId = repoResult.Data,
            EmployeeId = employeeId,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            FileType = ext,
            FileSize = finalFileSize,
            IsImage = isImg,
            UploadedDate = docEntity.UploadedDate
        };

        return new ApiResponse<EmployeeDocumentDto>
        {
            Code = "200",
            Message = "Document uploaded successfully.",
            Data = dto
        };
    }

    public async Task<ApiResponse<List<EmployeeDocumentDto>>> GetDocumentsByEmployeeIdAsync(int employeeId)
    {
        var repoResult = await _documentRepo.GetDocumentsByEmployeeIdAsync(employeeId);
        if (repoResult.Code != "200" || repoResult.Data == null)
        {
            return new ApiResponse<List<EmployeeDocumentDto>>
            {
                Code = repoResult.Code,
                Message = repoResult.Message,
                Data = new List<EmployeeDocumentDto>()
            };
        }

        var list = repoResult.Data.Select(d => new EmployeeDocumentDto
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

        return new ApiResponse<List<EmployeeDocumentDto>>
        {
            Code = "200",
            Message = "Documents retrieved successfully.",
            Data = list
        };
    }

    public async Task<(Stream stream, string contentType, string fileName)?> DownloadDocumentAsync(int documentId)
    {
        var repoResult = await _documentRepo.GetDocumentByIdAsync(documentId);
        if (repoResult.Code != "200" || repoResult.Data == null)
        {
            return null;
        }

        var doc = repoResult.Data;
        if (!File.Exists(doc.StoragePath))
        {
            return null;
        }

        var stream = await _fileStorage.GetFileStreamAsync(doc.StoragePath);
        string contentType = GetContentType(doc.OriginalFileName);
        return (stream, contentType, doc.OriginalFileName);
    }

    public async Task<(Stream zipStream, string zipFileName)?> DownloadAllDocumentsZipAsync(int employeeId)
    {
        var repoResult = await _documentRepo.GetDocumentsByEmployeeIdAsync(employeeId);
        if (repoResult.Code != "200" || repoResult.Data == null || repoResult.Data.Count == 0)
        {
            return null;
        }

        var zipMemoryStream = new MemoryStream();
        using (var archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var doc in repoResult.Data)
            {
                if (File.Exists(doc.StoragePath))
                {
                    string entryName = doc.OriginalFileName;
                    int counter = 1;
                    while (usedNames.Contains(entryName))
                    {
                        var nameOnly = Path.GetFileNameWithoutExtension(doc.OriginalFileName);
                        var ext = Path.GetExtension(doc.OriginalFileName);
                        entryName = $"{nameOnly}_{counter}{ext}";
                        counter++;
                    }
                    usedNames.Add(entryName);

                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    using (var entryStream = entry.Open())
                    using (var fileStream = File.OpenRead(doc.StoragePath))
                    {
                        await fileStream.CopyToAsync(entryStream);
                    }
                }
            }
        }

        zipMemoryStream.Position = 0;
        string zipFileName = $"Employee_{employeeId}_Documents.zip";
        return (zipMemoryStream, zipFileName);
    }

    public async Task<ApiResponse<bool>> DeleteDocumentAsync(int documentId)
    {
        var repoResult = await _documentRepo.GetDocumentByIdAsync(documentId);
        if (repoResult.Code != "200" || repoResult.Data == null)
        {
            return new ApiResponse<bool> { Code = "404", Message = "Document not found.", Data = false };
        }

        var path = repoResult.Data.StoragePath;
        var deleteResult = await _documentRepo.DeleteDocumentAsync(documentId);
        if (deleteResult.Code == "200")
        {
            _fileStorage.DeleteFile(path);
        }

        return deleteResult;
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".csv" => "text/csv",
            _ => "application/octet-stream"
        };
    }
}
