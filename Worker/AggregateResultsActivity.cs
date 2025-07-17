using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace FanOutFanIn;

[DurableTask]
public class AggregateResultsActivity : TaskActivity<Dictionary<string, int>[], Dictionary<string, int>>
{
    private readonly ILogger<AggregateResultsActivity> _logger;

    public AggregateResultsActivity(ILogger<AggregateResultsActivity> logger)
    {
        _logger = logger;
    }

    public override Task<Dictionary<string, int>> RunAsync(TaskActivityContext context, Dictionary<string, int>[] results)
    {
        _logger.LogInformation("Aggregating {Count} results", results.Length);
        
        Dictionary<string, int> aggregatedResult = new Dictionary<string, int>();
        
        foreach (Dictionary<string, int> result in results)
        {
            foreach (KeyValuePair<string, int> kvp in result)
            {
                aggregatedResult[kvp.Key] = kvp.Value;
            }
        }
        
        _logger.LogInformation("Aggregated {Count} work items into final result", aggregatedResult.Count);
        
        return Task.FromResult(aggregatedResult);
    }
}