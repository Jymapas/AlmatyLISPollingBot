using AlmatyLISPollingBot.Application.Features.Administrators;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using Microsoft.Extensions.DependencyInjection;

namespace AlmatyLISPollingBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PollCandidateSelectionService>();
        services.AddScoped<TournamentListFormatter>();
        services.AddScoped<PollCommandAuthorizer>();
        services.AddScoped<AdminSyncService>();

        return services;
    }
}
