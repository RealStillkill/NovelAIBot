using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NovelAIBot.Modules;

namespace NovelAIBot.Services
{
	internal class DiscordService : IHostedService, IHostedLifecycleService
	{
		private readonly ILogger<DiscordService> _logger;
		private readonly DiscordSocketClient _client;
		private readonly IHostApplicationLifetime _appLifetime;
		private readonly IConfiguration _configuration;
		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly InteractionService _interactionService;
		private readonly IServiceProvider _serviceProvider;

		public DiscordService(ILogger<DiscordService> logger, DiscordSocketClient client, IConfiguration configuration,
			IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory, IHostApplicationLifetime appLifetime,
			InteractionService interactionService)
		{
			_logger = logger;
			_client = client;
			_configuration = configuration;
			_serviceProvider = serviceProvider;
			_serviceScopeFactory = scopeFactory;
			_appLifetime = appLifetime;
			_interactionService = interactionService;

			_client.Ready += _client_Ready;
			_client.Log += Log;
			interactionService.InteractionExecuted += _interactionService_InteractionExecuted;
			_client.InteractionCreated += _client_InteractionCreated;
		}

		private async Task _client_Ready()
		{
			_logger.LogInformation("Client ready!");
			try
			{
				_logger.LogInformation("Registering commands");
				using (var scope = _serviceScopeFactory.CreateScope())
				{

					await _interactionService.AddModuleAsync<PromptModule>(scope.ServiceProvider);
					await _interactionService.RegisterCommandsGloballyAsync(true);
				}

				await _client.SetCustomStatusAsync(_configuration.GetRequiredSection("Discord")["CustomStatus"]);
			}
			catch (Exception ex)
			{
				_logger.LogCritical(ex, "A critical error has occurred while logging in.");
				_appLifetime.StopApplication();
			}
		}

		private async Task _client_InteractionCreated(SocketInteraction interaction)
		{
			try
			{
				_logger.LogInformation($"Interaction Received");
				var context = new SocketInteractionContext(_client, interaction);
				await _interactionService.ExecuteCommandAsync(context, _serviceProvider);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while executing a command.");
			}
			finally
			{
				_logger.LogInformation("Command execution finished.");
			}
		}

		private async Task _interactionService_InteractionExecuted(ICommandInfo commandInfo, IInteractionContext context, IResult result)
		{
			if (!result.IsSuccess)
			{
				switch (result.Error)
				{
					case InteractionCommandError.UnmetPrecondition:

						if (result.ErrorReason.Contains("User requires guild role"))
						{
							_logger.LogWarning($"User did not have the required role to execute {commandInfo.Module.Name}.{commandInfo.MethodName}");
							await context.Interaction.RespondAsync("Unauthorized.");
							return;
						}
						_logger.LogError($"Unmet Precondition - {result.ErrorReason}");
						break;
					case InteractionCommandError.Exception:
						_logger.LogError($"Exception - {result.ErrorReason} {result}");
						break;
					case InteractionCommandError.ConvertFailed:
						_logger.LogError($"Convert Failed - {result.ErrorReason}");
						break;
					case InteractionCommandError.ParseFailed:
						_logger.LogError($"Parse Failed - {result.ErrorReason}");
						break;
					case InteractionCommandError.Unsuccessful:
						_logger.LogError($"Unsuccessful - {result.ErrorReason}");
						break;
					case InteractionCommandError.BadArgs:
						_logger.LogError($"Bad Args - {result.ErrorReason}");
						break;
					case InteractionCommandError.UnknownCommand:
						_logger.LogError($"Unknown Command - {result.ErrorReason}");
						break;
					default:
						break;
				}
			}
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Logging in...");
			try
			{
				await _client.LoginAsync(TokenType.Bot, _configuration.GetSection("Discord")["Token"]);
				await _client.StartAsync();
				_logger.LogInformation("Log in complete.");
			}
			catch (Exception ex)
			{
				_logger.LogCritical(ex, "A critical error has occurred while logging in.");
				_appLifetime.StopApplication();
			}
		}
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Logging out...");
			await _client.LogoutAsync();
		}

		private Task Log(LogMessage arg)
		{
			switch (arg.Severity)
			{
				case LogSeverity.Critical:
					_logger.LogCritical(arg.Exception, arg.Message ?? "<No message>");
					break;
				case LogSeverity.Error:
					_logger.LogError(arg.Exception, arg.Message ?? "<No message>");
					break;
				case LogSeverity.Debug:
					_logger.LogDebug(arg.Exception, arg.Message ?? "<No message>");
					break;
				case LogSeverity.Verbose:
					_logger.LogInformation(arg.Exception, arg.Message ?? "<No message>");
					break;
				case LogSeverity.Warning:
					_logger.LogWarning(arg.Exception, arg.Message ?? "<No message>");
					break;
				case LogSeverity.Info:
					_logger.LogInformation(arg.Exception, arg.Message ?? "<No message>");
					break;
			}
			return Task.CompletedTask;
		}

		public Task StartedAsync(CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public Task StartingAsync(CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public Task StoppedAsync(CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public Task StoppingAsync(CancellationToken cancellationToken)
			=> Task.CompletedTask;
	}
}
