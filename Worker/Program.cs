using Microsoft.DurableTask.Worker;
using Microsoft.DurableTask.Worker.AzureManaged;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NumberCrunching.Worker;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

using ILoggerFactory loggerFactory = LoggerFactory.Create(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.SetMinimumLevel(LogLevel.Information);
});
ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

string endpoint = Environment.GetEnvironmentVariable("ENDPOINT") ?? "http://localhost:8080";
string taskHubName = Environment.GetEnvironmentVariable("TASKHUB") ?? "default";

string hostAddress = endpoint;
if (endpoint.Contains(';'))
{
    hostAddress = endpoint.Split(';')[0];
}

bool isLocalEmulator = endpoint == "http://localhost:8080";

string connectionString;
if (isLocalEmulator)
{
    connectionString = $"Endpoint={hostAddress};TaskHub={taskHubName};Authentication=None";
    logger.LogInformation("Using local emulator with no authentication");
}
else
{
    connectionString = $"Endpoint={hostAddress};TaskHub={taskHubName};Authentication=DefaultAzure";
    logger.LogInformation("Using Azure endpoint with DefaultAzure authentication");
}

logger.LogInformation("Using endpoint: {Endpoint}", endpoint);
logger.LogInformation("Using task hub: {TaskHubName}", taskHubName);
logger.LogInformation("Host address: {HostAddress}", hostAddress);
logger.LogInformation("Connection string: {ConnectionString}", connectionString);
logger.LogInformation("This worker uses parallel processing to find prime numbers");

builder.Services.AddDurableTaskWorker()
    .AddTasks(registry =>
    {
        registry.AddOrchestrator<PrimesOrchestration>();
        registry.AddActivity<CalculatePrimesActivity>();
    })
    .UseDurableTaskScheduler(connectionString);

IHost host = builder.Build();
logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Parallel Processing Worker");

await host.StartAsync();
logger.LogInformation("Worker started. Press any key to stop...");
Console.ReadKey(); 

await host.StopAsync();