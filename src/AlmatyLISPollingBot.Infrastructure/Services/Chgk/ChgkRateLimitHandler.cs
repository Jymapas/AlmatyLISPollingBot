namespace AlmatyLISPollingBot.Infrastructure.Services.Chgk;

internal sealed class ChgkRateLimitHandler : DelegatingHandler
{
    private readonly IChgkRequestPacer requestPacer;

    public ChgkRateLimitHandler(IChgkRequestPacer requestPacer)
    {
        this.requestPacer = requestPacer ?? throw new ArgumentNullException(nameof(requestPacer));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await requestPacer.ExecuteWhenAllowedAsync(
                () => base.SendAsync(request, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
