using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using Employee.ApiService.Models;

namespace Employee.FileService.Services;

/// <summary>
/// Сервис обработки сообщений из очереди SQS
/// и сохранения данных сотрудников в S3-хранилище
/// </summary>
/// <param name="sqs">Клиент для работы с очередью SQS</param>
/// <param name="s3">Клиент для работы с S3-хранилищем</param>
/// <param name="logger">Логгер сервиса</param>
public class SqsConsumerService(
    IAmazonSQS sqs,
    IAmazonS3 s3,
    ILogger<SqsConsumerService> logger)
    : BackgroundService
{
    private const string QueueName = "employee-queue";
    private const string BucketName = "employee-storage";

    /// <summary>
    /// Выполняет чтение сообщений из очереди SQS
    /// и сохраняет полученные данные в S3
    /// </summary>
    /// <param name="stoppingToken">Токен остановки фонового сервиса</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SQS consumer started");

        var queueUrl = string.Empty;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queueUrlResponse =
                    await sqs.GetQueueUrlAsync(QueueName, stoppingToken);

                queueUrl = queueUrlResponse.QueueUrl;

                break;
            }
            catch
            {
                logger.LogInformation("Waiting for SQS queue...");
                await Task.Delay(2000, stoppingToken);
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await sqs.ReceiveMessageAsync(
                    new ReceiveMessageRequest
                    {
                        QueueUrl = queueUrl,
                        MaxNumberOfMessages = 1,
                        WaitTimeSeconds = 5
                    },
                    stoppingToken);

                foreach (var message in response.Messages)
                {
                    var employee = JsonSerializer.Deserialize<EmployeeModel>(message.Body);

                    if (employee == null)
                    {
                        continue;
                    }

                    var fileName = $"employee_{employee.Id}.json";

                    await s3.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = BucketName,
                        Key = fileName,
                        ContentBody = message.Body
                    }, stoppingToken);

                    logger.LogInformation(
                        "File {FileName} uploaded to S3",
                        fileName);

                    await sqs.DeleteMessageAsync(
                        queueUrl,
                        message.ReceiptHandle,
                        stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while processing SQS messages");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}