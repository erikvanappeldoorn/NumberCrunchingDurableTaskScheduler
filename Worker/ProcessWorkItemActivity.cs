using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace NumberCrunching.Worker;

[DurableTask]
public class ProcessWorkItemActivity : TaskActivity<string, Dictionary<string, int>>
{
    private readonly ILogger<ProcessWorkItemActivity> _logger;

    public ProcessWorkItemActivity(ILogger<ProcessWorkItemActivity> logger)
    {
        _logger = logger;
    }

    public override Task<Dictionary<string, int>> RunAsync(TaskActivityContext context, string workItem)
    {
        _logger.LogInformation("Processing work item: {WorkItem}", workItem);
        
        Dictionary<string, int> result = new Dictionary<string, int>
        {
            { workItem, workItem.Length } // Simple example: Count characters in the work item
        };
        
        _logger.LogInformation("Completed processing work item: {WorkItem} with result: {Result}", 
            workItem, result[workItem]);
        
        return Task.FromResult(result);
    }
}