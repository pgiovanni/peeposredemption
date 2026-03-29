using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using peeposredemption.Application.Services;

namespace peeposredemption.Infrastructure.Services
{
    public class R2StorageService : IR2StorageService
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucketName;
        private readonly string _publicUrl;

        public R2StorageService(IConfiguration config)
        {
            var accountId = config["R2:AccountId"] ?? throw new InvalidOperationException("R2:AccountId not configured.");
            _bucketName = config["R2:BucketName"] ?? throw new InvalidOperationException("R2:BucketName not configured.");
            _publicUrl = (config["R2:PublicUrl"] ?? throw new InvalidOperationException("R2:PublicUrl not configured.")).TrimEnd('/');

            var accessKeyId = config["R2:AccessKeyId"] ?? throw new InvalidOperationException("R2:AccessKeyId not configured.");
            var secretAccessKey = config["R2:SecretAccessKey"] ?? throw new InvalidOperationException("R2:SecretAccessKey not configured.");

            var s3Config = new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true
            };

            _s3 = new AmazonS3Client(accessKeyId, secretAccessKey, s3Config);
        }

        public async Task<string> UploadEmojiAsync(string key, Stream imageStream, string contentType)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = imageStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead,
                UseChunkEncoding = false  // R2 doesn't support chunked streaming uploads
            };

            await _s3.PutObjectAsync(request);
            return $"{_publicUrl}/{key}";
        }

        public async Task<string> UploadArtistSampleAsync(string key, Stream imageStream, string contentType)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = imageStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead,
                UseChunkEncoding = false
            };

            await _s3.PutObjectAsync(request);
            return $"{_publicUrl}/{key}";
        }

        public async Task<string> UploadProfileImageAsync(string key, Stream imageStream, string contentType)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = imageStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead,
                UseChunkEncoding = false
            };

            await _s3.PutObjectAsync(request);
            return $"{_publicUrl}/{key}";
        }

        public async Task<string> UploadAttachmentAsync(string key, Stream stream, string contentType)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = $"attachments/{key}",
                InputStream = stream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead,
                UseChunkEncoding = false
            };

            await _s3.PutObjectAsync(request);
            return $"{_publicUrl}/attachments/{key}";
        }

        public async Task DeleteEmojiAsync(string key)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };
            await _s3.DeleteObjectAsync(request);
        }
    }
}
