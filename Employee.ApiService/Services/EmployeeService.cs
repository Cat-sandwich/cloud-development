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

        logger.LogInformation("Attempting to retrieve employee {EmployeeId} from cache", id);

        var cachedData = await cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            try
            {
                var cachedEmployee = JsonSerializer.Deserialize<EmployeeModel>(cachedData);

                if (cachedEmployee != null)
                {
                    logger.LogInformation("Employee {EmployeeId} retrieved from cache", id);
                    return cachedEmployee;
                }

                logger.LogWarning("Employee {EmployeeId} found in cache, but deserialization returned null", id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deserializing employee {EmployeeId}", id);
            }
        }

        logger.LogInformation("Employee {EmployeeId} not found in cache. Generating new one", id);

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

            logger.LogInformation("Employee {EmployeeId} saved to cache", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save employee {EmployeeId} to cache", id);
        }

        return employee;
    }

}
