# Number Crunching with the Durable Task Scheduler

## Description of the Demo

This demo demonstrates parallel processing (Fan-In/Fan-Out) on the Durable Task Scheduler using the .NET SDK. The Fan-Out Fan-In pattern represents a way to execute multiple operations in parallel and then aggregate the results, making it ideal for parallelized data processing scenarios.
You can run this demo on Azure with your own Azure Subscription or in a local environment using the Durable Task Scheduler emulator in Docker.


In this sample:
1. The orchestrator takes a max number (50.000.000) and a degree of parallism (20)
2. It then creates work item ranges e.g. (1 - 2.500.000), (2.500.001 - 5.000.000) etc..  
3. It fans out by creating a separate task for each work item using `CalculatePrimeActivity` 
4. Each task will calculate the number of primes that can be found in its work item.
5. All these tasks are executed in parallel
6. It waits for all tasks to complete using `Task.WhenAll`
7. It fans in by aggregating all individual results using `processingTasks.Sum(task => task.Result);`
8. The final aggregated result is returned to the client

This pattern is useful for:
- Processing large datasets in parallel for improved throughput
- Batch processing operations that can be executed independently
- Distributing computational workload across multiple workers
- Aggregating results from multiple sources or computations

## Prerequisites

1. [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
2. [Docker](https://www.docker.com/products/docker-desktop/) (for running the emulator) installed
3. [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (if using a deployed Durable Task Scheduler)

## Configuring Durable Task Scheduler

There are two ways to run this sample locally:

### Using the Emulator (Recommended)

The emulator simulates a scheduler and taskhub in a Docker container, making it ideal for development and learning.

1. Pull the Docker Image for the Emulator:
    ```bash
    docker pull mcr.microsoft.com/dts/dts-emulator:latest
    ```

1. Run the Emulator:
    ```bash
    docker run -it -p 8080:8080 -p 8082:8082 mcr.microsoft.com/dts/dts-emulator:latest
    ```
Wait a few seconds for the container to be ready.

Note: The example code automatically uses the default emulator settings (endpoint: http://localhost:8080, taskhub: default). You don't need to set any environment variables.

### Using a Deployed Scheduler and Taskhub in Azure

Local development with a deployed scheduler:

1. Install the durable task scheduler CLI extension:

    ```bash
    az upgrade
    az extension add --name durabletask --allow-preview true
    ```

1. Create a resource group in a region where the Durable Task Scheduler is available:

    ```bash
    az provider show --namespace Microsoft.DurableTask --query "resourceTypes[?resourceType=='schedulers'].locations | [0]" --out table
    ```

    ```bash
    az group create --name erikito-dts --location northeurope
    ```

1. Create a durable task scheduler resource:

    ```bash
    az durabletask scheduler create \
        --resource-group erikito-dts \
        --name erikito-dts \
        --ip-allowlist '["0.0.0.0/0"]' \
        --sku-name "Dedicated" \
        --sku-capacity 1
    ```

1. Create a task hub within the scheduler resource:

    ```bash
    az durabletask taskhub create \
        --resource-group erikito-dts \
        --scheduler-name erikito-dts \
        --name "default"
    ```

1. Grant the current user permission to connect to the `default` task hub:

    ```bash
    subscriptionId=$(az account show --query "id" -o tsv)
    loggedInUser=$(az account show --query "user.objectId" -o tsv)

    az role assignment create \
        --assignee $loggedInUser \
        --role "Durable Task Data Contributor" \
        --scope "/subscriptions/$subscriptionId/resourceGroups/erikito-dts/providers/Microsoft.DurableTask/schedulers/erikito-dts/taskHubs/default"
    ```

## Authentication

The sample includes smart detection of the environment and configures authentication automatically:

- For local development with the emulator (when endpoint is http://localhost:8080), no authentication is required.
- For local development with a deployed scheduler, DefaultAzure authentication is used, which utilizes DefaultAzureCredential behind the scenes and tries multiple authentication methods:
  - Managed Identity
  - Environment variables (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_CLIENT_SECRET)
  - Azure CLI login
  - Visual Studio login
  - and more

The connection string is constructed dynamically based on the environment:
```csharp
// For local emulator
connectionString = $"Endpoint={schedulerEndpoint};TaskHub={taskHubName};Authentication=None";

// For Azure deployed emulator
connectionString = $"Endpoint={schedulerEndpoint};TaskHub={taskHubName};Authentication=DefaultAzure";
```

## How to Run the Demo

Once you have set up either the emulator or deployed scheduler, follow these steps to run the sample:

1.  If you're using a deployed scheduler, you need to set Environment Variables:
    ```bash
    export ENDPOINT=$(az durabletask scheduler show \
        --resource-group erikito-dts \
        --name  erikito-dts \
        --query "properties.endpoint" \
        --output tsv)

    export TASKHUB="default"
    ```

1. Start the worker in a terminal:
    ```bash
    cd ./Worker
    dotnet run
    ```
    You should see output indicating the worker has started and registered the orchestration and activities.

1. In a new terminal, run the client:
    > **Note:** Remember to set the environment variables again if you're using a deployed scheduler. 

    ```bash
    cd ./Client
    dotnet run
    ```

## Identity-based authentication

Learn how to set up [identity-based authentication](https://learn.microsoft.com/azure/azure-functions/durable/durable-task-scheduler/durable-task-scheduler-identity?tabs=df&pivots=az-cli) when you deploy the app Azure.  

## Understanding the Code Structure

### Worker Project

The Worker project contains:

- **PrimesOrchestration.cs**: Defines the orchestrator
- **CalculatePrimesActivity.cs** Defines the Activity task
- **PrimeCrunher.cs** Defines methods for the caluculation of primes and creating work item ranges 
- **Program.cs**: Sets up the worker host with proper connection string handling

#### Orchestration Implementation

The orchestration uses the fan-out fan-in pattern by creating parallel activity tasks and waiting for all to complete:

```csharp
 public override async Task<int> RunAsync(TaskOrchestrationContext context, (int  max, byte parallel) input)
 {
     var cruncher = new PrimeCruncher();
     var boundaries = cruncher.GetBoundaries(input.max, input.parallel);
        
     var processingTasks = new List<Task<int>>();

     foreach (var boundary in boundaries)
     {
         processingTasks.Add(context.CallActivityAsync<int>(nameof(CalculatePrimesActivity), boundary));   
     }
        
     await Task.WhenAll(processingTasks);

     int total = processingTasks.Sum(task => task.Result);
     return total;
 }
```

The `CalculatePrimesActivity` is implemented as a separate class decorated with the `[DurableTask]` attribute:

```csharp
[DurableTask]
public class CalculatePrimesActivity : TaskActivity<(int start, int end), int>
{
    // Implementation processes a single work item
}

```

The worker uses Microsoft.Extensions.Hosting for proper lifecycle management:
```csharp
builder.Services.AddDurableTaskWorker()
    .AddTasks(registry =>
    {
        registry.AddOrchestrator<PrimesOrchestration>();
        registry.AddActivity<CalculatePrimesActivity>();
    })
    .UseDurableTaskScheduler(connectionString);
```

### Client Project

The Client project:

- Uses the same connection string logic as the worker
- Creates a max number and a degree of parallism to be processed in parallel
- Schedules an orchestration instance with the input
- Waits for the orchestration to complete and displays the aggregated results
- Uses WaitForInstanceCompletionAsync for efficient polling

```csharp
int max = 50_000_000;
byte parallel = 20;

logger.LogInformation("Starting finding number of primes in range 1-{max}", max);

string instanceId = await client.ScheduleNewOrchestrationInstanceAsync("PrimesOrchestration", (max, parallel));

logger.LogInformation("Started orchestration with ID: {InstanceId}", instanceId);
logger.LogInformation("Waiting for orchestration to complete...");

using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

OrchestrationMetadata instance = await client.WaitForInstanceCompletionAsync(instanceId, getInputsAndOutputs: true, cts.Token);

logger.LogInformation("Orchestration completed with status: {Status}", instance.RuntimeStatus);

```

## Understanding the Output

When you run the client, you should see:
1. The client starting an orchestration
2. The worker processing each work item in parallel
3. The worker aggregating all results
4. The client displaying the final aggregated results from the completed orchestration


When you run the sample, you'll see output from both the worker and client processes:

### Worker Output
The worker shows:
- Registration of the orchestrator and activities
- Log entries when each activity is called
- Parallel processing of multiple work items
- Final aggregation of results

### Client Output
The client shows:
- Starting the orchestration with a list of work items
- The unique orchestration instance ID
- The final aggregated results, showing each work item and its corresponding result
- Total count of processed items

This demonstrates the power of the Fan-Out Fan-In pattern for parallel processing and result aggregation.

## Reviewing the Orchestration in the Durable Task Scheduler Dashboard

To access the Durable Task Scheduler Dashboard and review your orchestration:

### Using the Emulator
1. Navigate to http://localhost:8082 in your web browser
2. Click on the "default" task hub
3. You'll see the orchestration instance in the list
4. Click on the instance ID to view the execution details, which will show:
   - The parallel execution of multiple activity tasks
   - The fan-in aggregation step
   - The input and output at each step
   - The time taken for each step

### Using a Deployed Scheduler
1. Navigate to the Scheduler resource in the Azure portal
2. Go to the Task Hub subresource that you're using
3. Click on the dashboard URL in the top right corner
4. Search for your orchestration instance ID
5. Review the execution details

The dashboard visualizes the Fan-Out Fan-In pattern, making it easy to see how tasks are distributed in parallel and then aggregated back together.
