using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("upload/{employeeId:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocuments(int employeeId, [FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new ApiResponse<List<EmployeeDocumentDto>>
            {
                Code = "400",
                Message = "No files were provided for upload.",
                Data = new List<EmployeeDocumentDto>()
            });
        }

        var uploaded = new List<EmployeeDocumentDto>();
        var errors = new List<string>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            using var stream = file.OpenReadStream();
            var res = await _documentService.UploadDocumentAsync(
                employeeId,
                stream,
                file.FileName,
                file.ContentType,
                file.Length);

            if (res.Code == "200" && res.Data != null)
            {
                uploaded.Add(res.Data);
            }
            else
            {
                errors.Add($"{file.FileName}: {res.Message}");
            }
        }

        if (uploaded.Count > 0)
        {
            return Ok(new ApiResponse<List<EmployeeDocumentDto>>
            {
                Code = "200",
                Message = errors.Count > 0
                    ? $"Uploaded {uploaded.Count} file(s) with some warnings: {string.Join("; ", errors)}"
                    : $"Uploaded {uploaded.Count} file(s) successfully.",
                Data = uploaded
            });
        }

        return BadRequest(new ApiResponse<List<EmployeeDocumentDto>>
        {
            Code = "400",
            Message = $"Failed to upload files: {string.Join("; ", errors)}",
            Data = uploaded
        });
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployeeId(int employeeId)
    {
        var result = await _documentService.GetDocumentsByEmployeeIdAsync(employeeId);
        return Ok(result);
    }

    [HttpGet("download/{documentId:int}")]
    public async Task<IActionResult> DownloadSingle(int documentId)
    {
        var download = await _documentService.DownloadDocumentAsync(documentId);
        if (download == null)
        {
            return NotFound(new ApiResponse<string> { Code = "404", Message = "Document not found.", Data = null });
        }

        return File(download.Value.stream, download.Value.contentType, download.Value.fileName);
    }

    [HttpGet("download-all/{employeeId:int}")]
    public async Task<IActionResult> DownloadAllZip(int employeeId)
    {
        var zipResult = await _documentService.DownloadAllDocumentsZipAsync(employeeId);
        if (zipResult == null)
        {
            return NotFound(new ApiResponse<string> { Code = "404", Message = "No documents found for this employee to download.", Data = null });
        }

        return File(zipResult.Value.zipStream, "application/zip", zipResult.Value.zipFileName);
    }

    [HttpDelete("{documentId:int}")]
    public async Task<IActionResult> Delete(int documentId)
    {
        var result = await _documentService.DeleteDocumentAsync(documentId);
        return StatusCode(int.TryParse(result.Code, out int code) ? code : 200, result);
    }
}
