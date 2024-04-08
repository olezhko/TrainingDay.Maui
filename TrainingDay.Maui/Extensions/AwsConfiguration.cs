using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using TrainingDay.Maui.Models.Settings;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.Extensions;

public static class AwsConfiguration
{
    public static void InitS3(this IServiceCollection services, S3BucketConfiguration configuration)
    {
        var awsOptions = new AWSOptions
        {
            Credentials = new BasicAWSCredentials(configuration.AccessKey, configuration.SecretKey),
            Region = RegionEndpoint.GetBySystemName(configuration.Region)
        };

        services.AddAWSService<IAmazonS3>(awsOptions, ServiceLifetime.Transient);
        services.AddTransient<IFileHelper, S3BucketFilesSevice>();
    }
}