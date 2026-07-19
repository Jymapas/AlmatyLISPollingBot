using AlmatyLISPollingBot.Application.Features.Administrators;
using AlmatyLISPollingBot.Application.Features.ExcludedTournaments;
using AlmatyLISPollingBot.Application.Features.ForcedTournaments;
using AlmatyLISPollingBot.Application.Features.Polls.Options;
using AlmatyLISPollingBot.Application.Features.Polls.Preview;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Application.Features.Polls.Results;
using Microsoft.Extensions.DependencyInjection;

namespace AlmatyLISPollingBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PollCandidateSelectionService>();
        services.AddScoped<PollCandidatePreparationService>();
        services.AddScoped<TournamentListFormatter>();
        services.AddScoped<ListTournamentOptionsService>();
        services.AddScoped<PreviewPollService>();
        services.AddScoped<PollCommandAuthorizer>();
        services.AddScoped<AdminSyncService>();
        services.AddScoped<BotSettingsSyncService>();
        services.AddScoped<UpdateSettingsService>();
        services.AddScoped<ExcludeTournamentsService>();
        services.AddScoped<UnexcludeTournamentsService>();
        services.AddScoped<ForceTournamentsService>();
        services.AddScoped<PollStateUpdateService>();
        services.AddScoped<PollResultsService>();

        return services;
    }
}
