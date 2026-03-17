using Employee.ApiService.Models;
using Employee.ApiService.Services;
using Employee.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("wasm", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddScoped<EmployeeService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseCors("wasm");
app.UseRouting();
app.MapGet("/api/employee", async (int id, EmployeeService service) =>
{
    var employee = await service.GetEmployeeAsync(id);
    return Results.Ok(employee);
})
.WithSummary("Получение сотрудника по идентификатору")
.WithDescription("Возвращает информацию о сотруднике по переданному id")
.Produces<EmployeeModel>(StatusCodes.Status200OK);

app.Run();