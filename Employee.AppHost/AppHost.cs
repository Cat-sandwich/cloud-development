var builder = DistributedApplication.CreateBuilder(args);

var redis = builder
    .AddRedis("redis")
    .WithRedisCommander();

var localstack = builder.AddContainer("localstack", "localstack/localstack:3.0")
    .WithEndpoint("localstack", e =>
    {
        e.TargetPort = 4566;
        e.UriScheme = "http";
    })
    .WithEnvironment("SERVICES", "s3,sqs")
    .WithEnvironment("DEFAULT_REGION", "us-east-1")
    .WaitFor(redis);

var localstackEndpoint = localstack.GetEndpoint("localstack");

var apiGateway = builder
    .AddProject<Projects.Employee_ApiGateway>("employee-apigateway")
    .WithHttpEndpoint(name: "gateway", port: 5200);

for (var i = 1; i <= 3; i++)
{
    var generator = builder
        .AddProject<Projects.Employee_ApiService>($"generator-{i}")
        .WithReference(redis)
        .WithEnvironment("AWS__ServiceURL", localstackEndpoint)
        .WaitFor(localstack)
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

builder.AddProject<Projects.Employee_FileService>("employee-fileservice")
    .WithReference(redis)
    .WithEnvironment("AWS__ServiceURL", localstackEndpoint)
    .WaitFor(redis)
    .WaitFor(localstack);

builder.Build().Run();