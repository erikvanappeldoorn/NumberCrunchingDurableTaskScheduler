namespace NumberCrunching.Worker;

internal class PrimeCruncher
{
    internal (long start, long end)[] GetBoundaries(long max, byte numberOfParallelTasks)
    {
        (long min, long max)[] boundaries = new (long min, long max)[numberOfParallelTasks];

        long range = max / numberOfParallelTasks;
        long start = 1;
        long end = range;

        for (int i = 0; i < numberOfParallelTasks; i++)
        {
            boundaries[i] = (start, end);
            start = start + range;
            end = end + range;
        }
        return boundaries;
    }

    internal bool IsPrime(long candidate)
    {
        if ((candidate & 1) != 0)
        {
            int num = (int)Math.Sqrt((double)candidate);
            for (int i = 3; i <= num; i += 2)
            {
                if (candidate % i == 0)
                {
                    return false;
                }
            }
            return true;
        }
        return candidate == 2;
    }
}