var builder = DistributedApplication.CreateBuilder(args);

var redis = builder
    .AddRedis("redis")
    .WithRedisCommander();

var apiGateway = builder
    .AddProject<Projects.Employee_ApiGateway>("employee-apigateway")
    .WithHttpEndpoint(name: "gateway", port: 5200);

for (var i = 1; i <= 3; i++)
{
    var generator = builder
        .AddProject<Projects.Employee_ApiService>($"generator-{i}")
        .WithReference(redis)
        .WaitFor(redis)
        .WithHttpEndpoint(name: $"http{i}", port: 5200 + i);

    apiGateway
        .WithReference(generator)
        .WaitFor(generator);
}

builder.AddProject<Projects.Client_Wasm>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.Build().Run();