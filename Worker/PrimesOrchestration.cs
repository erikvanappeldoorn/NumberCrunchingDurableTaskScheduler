using Microsoft.DurableTask;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NumberCrunching.Worker;

[DurableTask]
public class PrimesOrchestration : TaskOrchestrator< (int  max, byte parallel), int>
{
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
}