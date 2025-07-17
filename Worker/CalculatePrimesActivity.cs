using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace NumberCrunching.Worker;

[DurableTask]
public class CalculatePrimesActivity : TaskActivity<(int start, int end), int>
{
    private readonly ILogger<CalculatePrimesActivity> _logger;

    public CalculatePrimesActivity(ILogger<CalculatePrimesActivity> logger)
    {
        _logger = logger;
    }
    
    public override Task<int> RunAsync(TaskActivityContext context, (int start, int end ) boundary)
    {
        _logger.LogInformation("Start seeking for primes between: {BoundaryStart} and {BoundaryEnd}", boundary.start, boundary.end);
         var cruncher = new PrimeCruncher();
         
         int result = 0;
         for (long number = boundary.start; number <= boundary.end; number++)
         {
             if (cruncher.IsPrime(number))
             {
                 result++;
             }
         }
         
         _logger.LogInformation("{result} primes found between: {BoundaryStart} and {BoundaryEnd}", result, boundary.start, boundary.end);

        return Task.FromResult(result);
    }
}