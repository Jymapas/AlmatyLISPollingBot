using System.Net;
using AlmatyLISPollingBot.Infrastructure.Services.Chgk;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace AlmatyLISPollingBot.Application.Tests.Infrastructure;

public sealed class ChgkRequestPacerTests
{
    [Fact]
    public async Task ExecuteWhenAllowedAsync_ShouldDispatchRequestsAtHalfSecondIntervals()
    {
        var currentTimestamp = 0L;
        var delays = new List<TimeSpan>();
        var sut = new ChgkRequestPacer(
            () => currentTimestamp,
            timestampFrequency: 1_000,
            (delay, _) =>
            {
                delays.Add(delay);
                currentTimestamp += (long)Math.Ceiling(delay.TotalSeconds * 1_000);
                return Task.CompletedTask;
            });

        await sut.ExecuteWhenAllowedAsync(() => Task.FromResult(0), CancellationToken.None);
        await sut.ExecuteWhenAllowedAsync(() => Task.FromResult(0), CancellationToken.None);
        await sut.ExecuteWhenAllowedAsync(() => Task.FromResult(0), CancellationToken.None);

        delays.Should().Equal(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public async Task HttpPipeline_ShouldPaceEveryRetryAttempt()
    {
        var requestPacer = new RecordingRequestPacer();
        var transport = new SequencedResponseHandler(
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.OK);
        var services = new ServiceCollection();
        services.AddSingleton<IChgkRequestPacer>(requestPacer);
        services.AddTransient<ChgkRateLimitHandler>();

        var clientBuilder = services.AddHttpClient("chgk-test")
            .ConfigurePrimaryHttpMessageHandler(() => transport);
        clientBuilder.AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.Zero;
            options.Retry.UseJitter = false;
        });
        clientBuilder.AddHttpMessageHandler<ChgkRateLimitHandler>();

        using var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        using var client = httpClientFactory.CreateClient("chgk-test");

        using var response = await client.GetAsync("https://api.rating.chgk.info/tournaments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        transport.RequestCount.Should().Be(4);
        requestPacer.WaitCallCount.Should().Be(4);
    }

    private sealed class RecordingRequestPacer : IChgkRequestPacer
    {
        private int waitCallCount;

        public int WaitCallCount => waitCallCount;

        public Task<T> ExecuteWhenAllowedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref waitCallCount);
            return action();
        }
    }

    private sealed class SequencedResponseHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> responseStatuses;
        private int requestCount;

        public SequencedResponseHandler(params HttpStatusCode[] responseStatuses)
        {
            this.responseStatuses = new Queue<HttpStatusCode>(responseStatuses);
        }

        public int RequestCount => requestCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref requestCount);
            return Task.FromResult(new HttpResponseMessage(responseStatuses.Dequeue()));
        }
    }
}
