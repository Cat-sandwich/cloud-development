using Employee.ApiService.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Employee.ApiService.Services;

/// <summary>
/// Сервис получения сотрудников
/// </summary>
/// <param name="cache">кэш</param>
/// <param name="configuration">конфигурация</param>
/// <param name="logger">логирование</param>
public class EmployeeService(
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<EmployeeService> logger)
{
    /// <summary>
    /// Время жизни записи в кэше
    /// </summary>
    private readonly TimeSpan _cacheExpiration =
        TimeSpan.FromMinutes(configuration.GetValue("CacheSettings:ExpirationMinutes", 5));

    /// <summary>
    /// Получение сотрудника по id
    /// </summary>
    /// <param name="id">идентификатор</param>
    /// <returns></returns>
    public async Task<EmployeeModel> GetEmployeeAsync(int id)
    {
        var cacheKey = $"employee:{id}";

        logger.LogInformation("Попытка получить сотрудника {EmployeeId} из кэша", id);

        var cachedData = await cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            try
            {
                var cachedEmployee = JsonSerializer.Deserialize<EmployeeModel>(cachedData);

                if (cachedEmployee != null)
                {
                    logger.LogInformation("Сотрудник {EmployeeId} получен из кэша", id);
                    return cachedEmployee;
                }

                logger.LogWarning("Сотрудник {EmployeeId} найден в кэше, но десериализация вернула null", id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка десериализации сотрудника {EmployeeId}", id);
            }
        }

        logger.LogInformation("Сотрудник {EmployeeId} отсутствует в кэше. Генерация нового", id);

        var employee = EmployeeGenerator.Generate(id);

        try
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            };

            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(employee),
                cacheOptions
            );

            logger.LogInformation("Сотрудник {EmployeeId} сохранён в кэш", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось сохранить сотрудника {EmployeeId} в кэш", id);
        }

        return employee;
    }

}
