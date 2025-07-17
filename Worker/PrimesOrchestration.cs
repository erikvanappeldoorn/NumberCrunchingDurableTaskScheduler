using Microsoft.DurableTask;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NumberCrunching.Worker;

[DurableTask]
public class PrimesOrchestration : TaskOrchestrator< (int  max, byte parallel), int>
{
    private readonly ILogger<PrimesOrchestration> _logger;

    public PrimesOrchestration(ILogger<PrimesOrchestration> logger)
    {
        _logger = logger;
    }
    
    public override async Task<int> RunAsync(TaskOrchestrationContext context, (int  max, byte parallel) input)
    {
        var cruncher = new PrimeCruncher();
        var boundaries = cruncher.GetBoundaries(input.max, input.parallel);
        
        var processingTasks = new List<Task<int>>();

        foreach (var boundary in boundaries)
        {
            processingTasks.Add(context.CallActivityAsync<int>("CalculatePrimes", boundary));   
        }
        
        await Task.WhenAll(processingTasks);

        int total = processingTasks.Sum(task => task.Result);
        
        _logger.LogInformation("READY: {total} primes found in the range 1 - {max}", total, input.max); 
        
        return total;
    }
}