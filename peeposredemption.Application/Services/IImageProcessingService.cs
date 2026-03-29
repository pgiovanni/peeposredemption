namespace peeposredemption.Application.Services;

public interface IImageProcessingService
{
    /// <summary>
    /// Re-encodes the image (strips EXIF, neutralizes polyglots).
    /// Returns the processed stream and its canonical content type.
    /// </summary>
    Task<(Stream Output, string ContentType)> ProcessAsync(Stream input, string contentType);
}
