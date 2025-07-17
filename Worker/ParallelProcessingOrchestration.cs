using Microsoft.DurableTask;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FanOutFanIn;

[DurableTask]
public class ParallelProcessingOrchestration : TaskOrchestrator<List<string>, Dictionary<string, int>>
{
    public override async Task<Dictionary<string, int>> RunAsync(TaskOrchestrationContext context, List<string> workItems)
    {
        List<Task<Dictionary<string, int>>> processingTasks = new List<Task<Dictionary<string, int>>>();
        
        foreach (string workItem in workItems)
        {
            Task<Dictionary<string, int>> task = context.CallActivityAsync<Dictionary<string, int>>(
                nameof(ProcessWorkItemActivity), workItem);
            processingTasks.Add(task);
        }
        
        Dictionary<string, int>[] results = await Task.WhenAll(processingTasks);
        
        Dictionary<string, int> aggregatedResults = await context.CallActivityAsync<Dictionary<string, int>>(
            nameof(AggregateResultsActivity), results);
        
        return aggregatedResults;
    }
}