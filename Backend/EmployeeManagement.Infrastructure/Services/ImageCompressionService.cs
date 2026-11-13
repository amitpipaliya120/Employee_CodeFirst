using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Infrastructure.Services;

public class ImageCompressionService : IImageCompressionService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".webp"
    };

    private static readonly HashSet<string> ImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/bmp", "image/webp"
    };

    public bool IsImage(string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName);
        return (!string.IsNullOrEmpty(ext) && ImageExtensions.Contains(ext))
            || (!string.IsNullOrEmpty(contentType) && ImageMimeTypes.Contains(contentType));
    }

    public async Task<MemoryStream> CompressImageIfNeededAsync(Stream inputStream, string contentType, long maxSizeBytes = 1048576)
    {
        var outputStream = new MemoryStream();

        if (inputStream.CanSeek)
        {
            inputStream.Position = 0;
        }

        if (inputStream.Length <= maxSizeBytes)
        {
            await inputStream.CopyToAsync(outputStream);
            outputStream.Position = 0;
            return outputStream;
        }

        try
        {
            using (var image = await Image.LoadAsync(inputStream))
            {
                int maxDimension = 1920;
                if (image.Width > maxDimension || image.Height > maxDimension)
                {
                    var options = new ResizeOptions
                    {
                        Size = new Size(maxDimension, maxDimension),
                        Mode = ResizeMode.Max
                    };
                    image.Mutate(x => x.Resize(options));
                }

                int quality = 75;
                while (quality >= 30)
                {
                    outputStream.SetLength(0);
                    var encoder = new JpegEncoder { Quality = quality };
                    await image.SaveAsJpegAsync(outputStream, encoder);

                    if (outputStream.Length <= maxSizeBytes || quality <= 30)
                    {
                        break;
                    }
                    quality -= 15;
                }
            }

            outputStream.Position = 0;
            return outputStream;
        }
        catch
        {
            outputStream.SetLength(0);
            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }
            await inputStream.CopyToAsync(outputStream);
            outputStream.Position = 0;
            return outputStream;
        }
    }
}
