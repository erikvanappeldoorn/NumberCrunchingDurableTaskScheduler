using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.AzureManaged;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

ILogger<Program> logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("Starting Parallel Processing Client");

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
logger.LogInformation("This client starts the parallel processing of prime calculations");

ServiceCollection services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());

services.AddDurableTaskClient(options =>
{
    options.UseDurableTaskScheduler(connectionString);
});

ServiceProvider serviceProvider = services.BuildServiceProvider();
DurableTaskClient client = serviceProvider.GetRequiredService<DurableTaskClient>();

int max = 50_000_000;
byte parallel = 20;

logger.LogInformation("Starting finding number of primes in range 1-{max}", max);

string instanceId = await client.ScheduleNewOrchestrationInstanceAsync("PrimesOrchestration", (max, parallel));

logger.LogInformation("Started orchestration with ID: {InstanceId}", instanceId);
logger.LogInformation("Waiting for orchestration to complete...");

using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

OrchestrationMetadata instance = await client.WaitForInstanceCompletionAsync(instanceId, getInputsAndOutputs: true, cts.Token);

logger.LogInformation("Orchestration completed with status: {Status}", instance.RuntimeStatus);

if (instance.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
{
    int sum = instance.ReadOutputAs<int>();
    logger.LogInformation("READY: {sum} primes found", sum);
    
    logger.LogError("Orchestration completed");
}
else if (instance.RuntimeStatus == OrchestrationRuntimeStatus.Failed)
{
    logger.LogError("Orchestration failed: {ErrorMessage}", instance.FailureDetails?.ErrorMessage);
}

logger.LogInformation("Client ready. Press any key to stop...");
Console.ReadKey(); 