using Employee.ApiService.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Employee.IntegrationTests;

/// <summary>
/// Интеграционные тесты микросервисного приложения
/// </summary>
/// <param name="fixture">Фикстура для запуска Aspire-окружения</param>
public class IntegrationTests(Fixture fixture)
    : IClassFixture<Fixture>
{
    /// <summary>
    /// Проверка сохранения файла сотрудника в S3
    /// </summary>
    [Fact]
    public async Task EmployeeSavedToS3()
    {
        var testId = Random.Shared.Next(1, 100000);

        var response =
            await fixture.GatewayClient.GetAsync(
                $"/api/employee?id={testId}");

        response.EnsureSuccessStatusCode();

        var expectedFile =
            $"employee_{testId}.json";

        var files =
            await fixture.WaitForS3ObjectAsync(expectedFile);

        Assert.NotEmpty(files);
    }

    /// <summary>
    /// Проверка корректного получения сотрудника
    /// </summary>
    [Fact]
    public async Task GetEmployee_ReturnsCorrectId()
    {
        var testId = Random.Shared.Next(1, 100000);

        var response =
            await fixture.GatewayClient.GetAsync(
                $"/api/employee?id={testId}");

        response.EnsureSuccessStatusCode();

        var content =
            await response.Content.ReadAsStringAsync();

        using var doc =
            JsonDocument.Parse(content);

        var id =
            doc.RootElement
                .GetProperty("id")
                .GetInt32();

        Assert.Equal(testId, id);
    }

    /// <summary>
    /// Проверка корректности данных сотрудника
    /// </summary>
    [Fact]
    public async Task Employee_HasValidData()
    {
        var testId = Random.Shared.Next(1, 100000);

        var response =
            await fixture.GatewayClient.GetAsync(
                $"/api/employee?id={testId}");

        response.EnsureSuccessStatusCode();

        var employee =
            await response.Content
                .ReadFromJsonAsync<EmployeeModel>();

        Assert.NotNull(employee);
        Assert.Equal(testId, employee.Id);
        Assert.False(string.IsNullOrWhiteSpace(employee.Name));
        Assert.False(string.IsNullOrWhiteSpace(employee.Email));
        Assert.False(string.IsNullOrWhiteSpace(employee.Phone));
        Assert.False(string.IsNullOrWhiteSpace(employee.Position));
        Assert.False(string.IsNullOrWhiteSpace(employee.Department));
        Assert.True(employee.Salary > 0);
        Assert.Equal(
            employee.DismissalIndicator,
            employee.DateDismissal is not null);
    }

    /// <summary>
    /// Проверка отсутствия дублирования файлов сотрудников
    /// </summary>
    [Fact]
    public async Task RepeatedRequests_DoNotCreateDuplicateFiles()
    {
        var testId = Random.Shared.Next(1, 100000);

        await fixture.GatewayClient.GetAsync(
            $"/api/employee?id={testId}");

        await fixture.GatewayClient.GetAsync(
            $"/api/employee?id={testId}");

        await Task.Delay(5000);

        var response =
            await fixture.S3Client.ListObjectsV2Async(
                new Amazon.S3.Model.ListObjectsV2Request
                {
                    BucketName = "employee-storage",
                    Prefix = $"employee_{testId}"
                });

        Assert.Single(response.S3Objects);
    }

    /// <summary>
    /// Проверка получения файла сотрудника
    /// </summary>
    [Fact]
    public async Task EmployeeFile_CanBeRetrieved()
    {
        var testId = Random.Shared.Next(1, 100000);

        await fixture.GatewayClient.GetAsync(
            $"/api/employee?id={testId}");

        var fileName =
            $"employee_{testId}.json";

        await fixture.WaitForS3ObjectAsync(fileName);

        var response =
            await fixture.FileServiceClient.GetAsync(
                $"/files/{fileName}");

        response.EnsureSuccessStatusCode();

        var content =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(testId.ToString(), content);
    }
}