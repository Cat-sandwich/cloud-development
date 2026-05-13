using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Employee.FileService.Services;
using Employee.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var awsUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566";

builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    return new AmazonSQSClient(
        new BasicAWSCredentials("test", "test"),
        new AmazonSQSConfig
        {
            ServiceURL = awsUrl,
            AuthenticationRegion = "us-east-1"
        });
});

builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    return new AmazonS3Client(
        new BasicAWSCredentials("test", "test"),
        new AmazonS3Config
        {
            ServiceURL = awsUrl,
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1"
        });
});

builder.Services.AddHostedService<SqsConsumerService>();
builder.Services.AddHostedService<AwsInitializerService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.MapGet("/files", async (IAmazonS3 s3) =>
{
    var response = await s3.ListObjectsV2Async(
        new ListObjectsV2Request
        {
            BucketName = "employee-storage"
        });

    var files = response.S3Objects?
        .Select(x => x.Key)
        .ToList();

    if (files == null || files.Count == 0)
    {
        return Results.Ok(new
        {
            Message = "Файлы отсутствуют"
        });
    }

    return Results.Ok(files);
});
app.MapGet("/files/{key}", async (string key, IAmazonS3 s3) =>
{
    try
    {
        var response = await s3.GetObjectAsync(
            "employee-storage",
            key);

        using var reader =
            new StreamReader(response.ResponseStream);

        var content = await reader.ReadToEndAsync();

        return Results.Text(content, "application/json");
    }
    catch
    {
        return Results.NotFound();
    }
});

app.Run();