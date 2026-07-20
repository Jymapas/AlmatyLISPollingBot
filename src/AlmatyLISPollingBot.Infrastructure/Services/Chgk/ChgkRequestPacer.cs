using System.Diagnostics;

namespace AlmatyLISPollingBot.Infrastructure.Services.Chgk;

internal sealed class ChgkRequestPacer : IChgkRequestPacer
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Func<long> getTimestamp;
    private readonly long timestampFrequency;
    private readonly Func<TimeSpan, CancellationToken, Task> delayAsync;
    private long nextAllowedTimestamp;

    public ChgkRequestPacer()
        : this(Stopwatch.GetTimestamp, Stopwatch.Frequency, Task.Delay)
    {
    }

    internal ChgkRequestPacer(
        Func<long> getTimestamp,
        long timestampFrequency,
        Func<TimeSpan, CancellationToken, Task> delayAsync)
    {
        ArgumentNullException.ThrowIfNull(getTimestamp);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timestampFrequency, 0);
        ArgumentNullException.ThrowIfNull(delayAsync);

        this.getTimestamp = getTimestamp;
        this.timestampFrequency = timestampFrequency;
        this.delayAsync = delayAsync;
    }

    public async Task<T> ExecuteWhenAllowedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        Task<T> execution;
        try
        {
            var currentTimestamp = getTimestamp();
            var scheduledTimestamp = Math.Max(currentTimestamp, nextAllowedTimestamp);
            nextAllowedTimestamp = checked(scheduledTimestamp + GetMinimumIntervalTicks(timestampFrequency));
            var delay = GetDelay(currentTimestamp, scheduledTimestamp);

            if (delay > TimeSpan.Zero)
            {
                await delayAsync(delay, cancellationToken).ConfigureAwait(false);
            }

            execution = action();
        }
        finally
        {
            gate.Release();
        }

        return await execution.ConfigureAwait(false);
    }

    private TimeSpan GetDelay(long currentTimestamp, long scheduledTimestamp)
    {
        return TimeSpan.FromSeconds((scheduledTimestamp - currentTimestamp) / (double)timestampFrequency);
    }

    private static long GetMinimumIntervalTicks(long frequency)
    {
        return checked((frequency / 2) + (frequency % 2));
    }
}
