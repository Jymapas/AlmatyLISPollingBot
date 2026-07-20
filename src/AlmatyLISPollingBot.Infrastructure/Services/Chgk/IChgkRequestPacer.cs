namespace AlmatyLISPollingBot.Infrastructure.Services.Chgk;

internal interface IChgkRequestPacer
{
    Task<T> ExecuteWhenAllowedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken);
}
