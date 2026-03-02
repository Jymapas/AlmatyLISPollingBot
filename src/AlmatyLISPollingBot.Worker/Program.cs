using AlmatyLISPollingBot.Application;
using AlmatyLISPollingBot.Application.Abstractions.Messaging;
using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Application.Features.MakePost;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Application.Features.Polls.StopPoll;
using AlmatyLISPollingBot.Infrastructure;
using AlmatyLISPollingBot.Worker.Configuration;
using AlmatyLISPollingBot.Worker.HostedServices;
using AlmatyLISPollingBot.Worker.Telegram;
using Microsoft.Extensions.Options;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);
ConfigurationBootstrapper.ConfigureAppConfiguration(builder.Configuration);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOptions<TelegramBotConfiguration>()
    .Bind(builder.Configuration.GetSection(TelegramBotConfiguration.SectionName))
    .Validate(x => !string.IsNullOrWhiteSpace(x.BotToken), "Telegram bot token is required.")
    .ValidateOnStart();

builder.Services.AddOptions<BotConfiguration>()
    .Bind(builder.Configuration.GetSection(BotConfiguration.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<ITelegramBotClient>(serviceProvider =>
{
    var telegramSettings = serviceProvider.GetRequiredService<IOptions<TelegramBotConfiguration>>().Value;
    return new TelegramBotClient(telegramSettings.BotToken);
});

builder.Services.AddScoped<StartPollService>();
builder.Services.AddScoped<StopPollService>();
builder.Services.AddScoped<MakePostService>();
builder.Services.AddScoped<TelegramUpdateRouter>();
builder.Services.AddScoped<IChatBotClient, TelegramMainAdminClient>();

builder.Services.AddHostedService<AdminSyncSchedulerService>();
builder.Services.AddHostedService<TelegramLongPollingService>();

var host = builder.Build();
await host.RunAsync();
