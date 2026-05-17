using Amazon.SQS;
using Amazon.SQS.Model;
using Employee.ApiService.Models;
using System.Text.Json;

namespace Employee.ApiService.Services;
/// <summary>
/// Сервис отправки сотрудников в SQS очередь
/// </summary>
/// <param name="sqs">клиент Amazon SQS</param>
/// <param name="logger">логирование сервиса</param>
public class SqsPublisherService(
    IAmazonSQS sqs,
    ILogger<SqsPublisherService> logger)
{
    /// <summary>
    /// Настройки сериализации JSON для отправки сообщений в очередь
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    /// <summary>
    /// URL очереди SQS, кешируемый после первого успешного получения
    /// </summary>
    private string? _queueUrl;

    /// <summary>
    /// Отправка сотрудника в SQS очеред
    /// </summary>
    /// <param name="employee">данные сотрудника</param>
    public async Task PublishAsync(EmployeeModel employee)
    {
        if(_queueUrl is null)
        {
            while (true)
            {
                try
                {
                    _queueUrl = (await sqs.GetQueueUrlAsync("employee-queue")).QueueUrl;
                    break;
                }
                catch
                {
                    logger.LogInformation("Waiting for SQS queue...");
                    await Task.Delay(2000);
                }
            }
        }

        var json = JsonSerializer.Serialize(employee, _jsonOptions);

        await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = json
        });

        logger.LogInformation(
            "Employee {Id} sent to SQS",
            employee.Id);
    }
}