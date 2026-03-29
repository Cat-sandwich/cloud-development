using Employee.ApiGateway.LoadBalancer;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var generators = builder.Configuration.GetSection("Generators").Get<string[]>() ?? [];

var overrides = new List<KeyValuePair<string, string?>>();

for (var i = 0; i < generators.Length; i++)
{
    var serviceName = generators[i];
    var url = builder.Configuration[$"services:{serviceName}:http:0"];

    if (string.IsNullOrWhiteSpace(url))
        continue;

    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        continue;

    overrides.Add(new KeyValuePair<string, string?>(
        $"Routes:0:DownstreamHostAndPorts:{i}:Host", uri.Host));

    overrides.Add(new KeyValuePair<string, string?>(
        $"Routes:0:DownstreamHostAndPorts:{i}:Port", uri.Port.ToString()));
}

if (overrides.Any())
{
    builder.Configuration.AddInMemoryCollection(overrides);
}

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer((route, sp) =>
        new QueryBasedLoadBalancer(sp));

var app = builder.Build();

await app.UseOcelot();
await app.RunAsync();