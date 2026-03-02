using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using Microsoft.Extensions.DependencyInjection;

namespace AlmatyLISPollingBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PollCandidateSelectionService>();

        return services;
    }
}
