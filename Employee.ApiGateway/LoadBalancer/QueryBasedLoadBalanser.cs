using Ocelot.Values;
using Ocelot.Responses;
using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.ServiceDiscovery.Providers;

namespace Employee.ApiGateway.LoadBalancer;

/// <summary>
/// Балансировщик нагрузки, выбирающий реплику по значению параметра "id"
/// </summary>
/// <param name="serviceDiscovery">Провайдер для получения списка доступных сервисов</param>
public class QueryBasedLoadBalancer(IServiceDiscoveryProvider serviceDiscovery) : ILoadBalancer
{
    private const string IdQuery = "id";

    public string Type => nameof(QueryBasedLoadBalancer);

    /// <summary>
    /// Функция выбора сервиса по параметру "id"
    /// </summary>
    /// <param name="httpContext">Контекст HTTP-запроса</param>
    /// <returns>Адрес выбранного сервиса или ошибка</returns>
    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await serviceDiscovery.GetAsync();

        if (services is null || services.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(
                new ServicesAreNullError("Нет доступных сервисов"));
        }

        var idResult = TryGetValidId(httpContext.Request.Query);

        if (!idResult.IsSuccess)
        {
            return new ErrorResponse<ServiceHostAndPort>(
                new UnableToFindLoadBalancerError(idResult.ErrorMessage));
        }

        var id = idResult.Value;

        var index = id % services.Count;
        var selected = services[index];

        return new OkResponse<ServiceHostAndPort>(selected.HostAndPort);
    }

    /// <summary>
    /// Функция проверки параметра запроса
    /// </summary>
    /// <param name="query">Запрос</param>
    /// <returns>Значение id и сообщение об ошибке</returns>
    private static (bool IsSuccess, int Value, string ErrorMessage) TryGetValidId(IQueryCollection query)
    {
        if (!query.TryGetValue(IdQuery, out var idValues) || string.IsNullOrWhiteSpace(idValues))
            return (false, 0, "Отсутствует или пустой параметр 'id'");

        if (!int.TryParse(idValues.First(), out var id))
            return (false, 0, "Параметр 'id' должен быть числом");

        if (id < 0)
            return (false, 0, "Параметр 'id' не может быть отрицательным");

        return (true, id, string.Empty);
    }
    public void Release(ServiceHostAndPort hostAndPort) { }
}