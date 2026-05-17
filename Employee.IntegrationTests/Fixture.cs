using Amazon.Runtime;
using Amazon.S3;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Amazon.S3.Model;

namespace Employee.IntegrationTests;

public class Fixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; } = null!;
    public AmazonS3Client S3Client { get; private set; } = null!;
    public HttpClient GatewayClient { get; private set; } = null!;
    public HttpClient FileServiceClient { get; private set; } = null!;

    private const string BucketName = "employee-storage";

    public async Task InitializeAsync() 
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Employee_AppHost>(
            [
                "DcpPublisher:RandomizePorts=false"
            ]);

        App = await appHost.BuildAsync();
        await App.StartAsync(cts.Token);

        await Task.WhenAll(
            App.ResourceNotifications.WaitForResourceAsync("localstack"),
            App.ResourceNotifications.WaitForResourceAsync("redis"),
            App.ResourceNotifications.WaitForResourceAsync("employee-apigateway"),
            App.ResourceNotifications.WaitForResourceAsync("employee-fileservice")
        ).WaitAsync(TimeSpan.FromMinutes(5));

        await Task.Delay(TimeSpan.FromSeconds(10));

        GatewayClient = App.CreateHttpClient("employee-apigateway");
        Assert.NotNull(GatewayClient);

        FileServiceClient = App.CreateHttpClient("employee-fileservice");

        var localstackEndpoint = App.GetEndpoint("localstack", "localstack");
        var localstackUrl = $"http://{localstackEndpoint.Host}:{localstackEndpoint.Port}";

        S3Client = new AmazonS3Client(
            new BasicAWSCredentials("test", "test"),
            new AmazonS3Config
            {
                ServiceURL = localstackUrl,
                ForcePathStyle = true,
                UseHttp = true,
                AuthenticationRegion = "us-east-1"
            });
    }

    public async Task DisposeAsync()  
    {
        GatewayClient?.Dispose();
        FileServiceClient?.Dispose();
        S3Client?.Dispose();

        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }

    public async Task<List<S3Object>> WaitForS3ObjectAsync(string key)
    {
        for (var i = 0; i < 15; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            try
            {
                var response = await S3Client.ListObjectsV2Async(
                    new ListObjectsV2Request
                    {
                        BucketName = BucketName,
                        Prefix = key
                    });

                if (response.S3Objects is not null && response.S3Objects.Count > 0)
                {
                    return response.S3Objects;
                }
            }
            catch (AmazonS3Exception)  
            {
                
            }
        }

        return [];
    }
}
