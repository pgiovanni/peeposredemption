namespace peeposredemption.Application.Services
{
    public interface IR2StorageService
    {
        Task<string> UploadEmojiAsync(string key, Stream imageStream, string contentType);
        Task DeleteEmojiAsync(string key);
    }
}
