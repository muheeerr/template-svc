using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.S3
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddS3Helper(this IServiceCollection services, string accessKey, string secretKey, string bucketName, string region)
        {
            services.AddSingleton(new S3Helper(accessKey, secretKey, bucketName, region));
            return services;
        }
    }
}
