using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace Utility.Helpers
{
    public class S3Helper
    {
        private readonly string _bucketName;
        private readonly IAmazonS3 _s3Client;

        public S3Helper(string accessKey, string secretKey, string bucketName, string region)
        {
            _bucketName = bucketName;
            _s3Client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(region));
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileName,
                BucketName = _bucketName,
                ContentType = contentType,
                //CannedACL = S3CannedACL.PublicRead
            };

            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);
            return $"https://{_bucketName}.s3.amazonaws.com/{fileName}";
        }
        public async Task<string> PutFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var response = await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = fileStream,
                ContentType = contentType,
                AutoCloseStream = false
            });
            return $"https://{_bucketName}.s3.amazonaws.com/{fileName}";
        }
    }
}
