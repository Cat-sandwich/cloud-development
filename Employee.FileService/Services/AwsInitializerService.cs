using Amazon.S3;
using Amazon.SQS;

namespace Employee.FileService.Services;

/// <summary>
/// Инициализирует AWS-ресурсы в LocalStack
/// </summary>
/// <param name="s3">Клиент для работы с S3-хранилищем</param>
/// <param name="sqs">Клиент для работы с очередью SQS</param>
/// <param name="logger">Логгер сервиса</param>
public class AwsInitializerService(
    IAmazonS3 s3,
    IAmazonSQS sqs,
    ILogger<AwsInitializerService> logger)
    : IHostedService
{

    /// <summary>
    /// Создает необходимые ресурсы AWS и ожидает готовности LocalStack
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                try
                {
                    await s3.PutBucketAsync("employee-storage", cancellationToken);

                    logger.LogInformation("S3 bucket created");
                }
                catch (AmazonS3Exception ex)
                    when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
                {
                    logger.LogInformation("S3 bucket already exists");
                }

                await sqs.CreateQueueAsync("employee-queue", cancellationToken);

                logger.LogInformation("SQS queue created");

                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "LocalStack not ready yet");

                await Task.Delay(3000, cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}