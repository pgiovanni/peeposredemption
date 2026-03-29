using peeposredemption.Application.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace peeposredemption.Infrastructure.Services;

public class ImageProcessingService : IImageProcessingService
{
    public async Task<(Stream Output, string ContentType)> ProcessAsync(Stream input, string contentType)
    {
        using var image = await Image.LoadAsync(input);

        // Strip all metadata (EXIF, ICC profiles, etc.)
        image.Metadata.ExifProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        var output = new MemoryStream();

        // Re-encode to a canonical format — always output as PNG for lossless, or JPEG for large photos
        string outContentType;
        if (contentType.Contains("png") || contentType.Contains("gif") || contentType.Contains("webp"))
        {
            await image.SaveAsPngAsync(output, new PngEncoder());
            outContentType = "image/png";
        }
        else
        {
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 88 });
            outContentType = "image/jpeg";
        }

        output.Position = 0;
        return (output, outContentType);
    }
}
