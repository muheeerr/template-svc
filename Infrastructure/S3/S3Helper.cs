using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;

namespace Infrastructure.S3
{
    public class S3Helper
    {
        private readonly string _bucketName;
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<S3Helper>? _logger;

        public S3Helper(string accessKey, string secretKey, string bucketName, string region, ILogger<S3Helper>? logger = null)
        {
            _bucketName = bucketName;
            _s3Client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(region));
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileName,
                BucketName = _bucketName,
                ContentType = contentType,
            };

            try
            {
                var fileTransferUtility = new TransferUtility(_s3Client);
                await fileTransferUtility.UploadAsync(uploadRequest);
                var url = $"https://{_bucketName}.s3.amazonaws.com/{fileName}";
                _logger?.LogInformation("S3 upload succeeded {Bucket} {Key} {ContentType}", _bucketName, fileName, contentType);
                return url;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "S3 upload failed {Bucket} {Key} {ContentType}", _bucketName, fileName, contentType);
                throw;
            }
        }

        public async Task<string> PutFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                var response = await _s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileName,
                    InputStream = fileStream,
                    ContentType = contentType,
                    AutoCloseStream = false
                });
                var url = $"https://{_bucketName}.s3.amazonaws.com/{fileName}";
                _logger?.LogInformation("S3 put succeeded {Bucket} {Key} {ContentType}", _bucketName, fileName, contentType);
                return url;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "S3 put failed {Bucket} {Key} {ContentType}", _bucketName, fileName, contentType);
                throw;
            }
        }
    }
}
