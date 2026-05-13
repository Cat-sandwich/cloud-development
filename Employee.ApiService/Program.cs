using Employee.ApiService.Models;
using Employee.ApiService.Services;
using Employee.ServiceDefaults;
using Amazon.SQS;
using Amazon.Runtime;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddEndpointsApiExplorer();

var awsUrl = builder.Configuration["AWS:ServiceURL"];

builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    return new AmazonSQSClient(
        new BasicAWSCredentials("test", "test"),
        new AmazonSQSConfig
        {
            ServiceURL = awsUrl
        });
});

builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);
});


builder.Services.AddScoped<EmployeeService>();
builder.Services.AddSingleton<SqsPublisherService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.MapGet("/", () => Results.Redirect("/files"));
app.UseHttpsRedirection();
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