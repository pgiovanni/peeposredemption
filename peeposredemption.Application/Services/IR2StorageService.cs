namespace peeposredemption.Application.Services
{
    public interface IR2StorageService
    {
        Task<string> UploadEmojiAsync(string key, Stream imageStream, string contentType);
        Task DeleteEmojiAsync(string key);
        Task<string> UploadArtistSampleAsync(string key, Stream imageStream, string contentType);
        Task<string> UploadProfileImageAsync(string key, Stream imageStream, string contentType);
        Task<string> UploadAttachmentAsync(string key, Stream stream, string contentType);
    }
}
