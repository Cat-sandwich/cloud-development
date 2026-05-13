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
    private const string QueueName = "employee-queue";

    /// <summary>
    /// Отправка сотрудника в SQS очеред
    /// </summary>
    /// <param name="employee">данные сотрудника</param>
    public async Task PublishAsync(EmployeeModel employee)
    {
        var queueUrl = string.Empty;

        while (true)
        {
            try
            {
                var response = await sqs.GetQueueUrlAsync("employee-queue");

                queueUrl = response.QueueUrl;

                break;
            }
            catch
            {
                logger.LogInformation("Waiting for SQS queue...");
                await Task.Delay(2000);
            }
        }

        var json = JsonSerializer.Serialize(
            employee,
            new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            });

        await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = json
        });

        logger.LogInformation(
            "Employee {Id} sent to SQS",
            employee.Id);
    }
}