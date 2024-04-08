using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using TrainingDay.Maui.Models.AWS;

namespace TrainingDay.Maui.Services;

internal class S3BucketFilesSevice(IAmazonS3 amazonS3Client) : IFileHelper
{
    public async Task<IEnumerable<S3ObjectDto>> GetFilesList()
    {
        var request = new ListObjectsV2Request()
        {
            BucketName = "trainingday-exercise-images",
        };

        var result = await amazonS3Client.ListObjectsV2Async(request);
        var s3Objects = result.S3Objects.Select(s =>
        {
            var urlRequest = new GetPreSignedUrlRequest()
            {
                BucketName = "trainingday-exercise-images",
                Key = s.Key,
                Expires = DateTime.MaxValue
            };
            return new S3ObjectDto()
            {
                Name = s.Key.ToString(),
                PresignedUrl = amazonS3Client.GetPreSignedURL(urlRequest),
            };
        });

        return s3Objects;
    }

    public async Task<string> GetFile(string key)
    {
        var response = await amazonS3Client.GetObjectAsync("trainingday-exercise-images", key);

        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.Combine(documentsPath, key);

        if (response.HttpStatusCode == HttpStatusCode.OK)
        {
            await response.WriteResponseStreamToFileAsync(path, false, CancellationToken.None);
        }

        return path;
    }
}