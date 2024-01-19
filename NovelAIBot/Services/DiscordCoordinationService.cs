using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovelAIBot.Services
{
	internal class DiscordCoordinationService
	{
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly DiscordSocketClient _client;
		private readonly IConfiguration _configuration;
		private readonly ILogger _logger;

		public DiscordCoordinationService(IServiceScopeFactory scopeFactory, DiscordSocketClient client, IConfiguration configuration, ILogger logger)
		{
			_scopeFactory = scopeFactory;
			_client = client;
			_configuration = configuration;
			_logger = logger;


			_client.Ready += _client_Ready;
			_client.SlashCommandExecuted += _client_SlashCommandExecuted;
			_client.ButtonExecuted += _client_ButtonExecuted;
			_client.Log += _client_Log;

			_ = Task.Factory.StartNew(async () =>
			{
				_logger.Information("Logging in..");
				string token = _configuration["Discord"];
				await _client.LoginAsync(Discord.TokenType.Bot, token);
				await _client.StartAsync();
				_logger.Information("Logged in");
			});
		}

		private async Task _client_Log(Discord.LogMessage arg)
		{
			switch (arg.Severity)
			{
				case Discord.LogSeverity.Critical:
				case Discord.LogSeverity.Error:
					_logger.Error(arg.Exception, $"{arg.Source} - {arg.Message}");
					break;
				case Discord.LogSeverity.Warning:
					_logger.Warning(arg.Exception, $"{arg.Source} - {arg.Message}");
					break;
				case Discord.LogSeverity.Info:
					_logger.Information(arg.Exception, $"{arg.Source} - {arg.Message}");
					break;
				case Discord.LogSeverity.Verbose:
					_logger.Verbose(arg.Exception, $"{arg.Source} - {arg.Message}");
					break;
				case Discord.LogSeverity.Debug:
					_logger.Debug(arg.Exception, $"{arg.Source} - {arg.Message}");
					break;
			}
		}

		private async Task _client_ButtonExecuted(SocketMessageComponent arg)
		{
			_ = Task.Factory.StartNew(async () =>
			{
				using (IServiceScope scope = _scopeFactory.CreateScope())
				{
					DiscordInteractionService intService = scope.ServiceProvider.GetRequiredService<DiscordInteractionService>();
					await intService.HandleButtonAsync(arg);
				}
			});
		}

		private async Task _client_SlashCommandExecuted(SocketSlashCommand arg)
		{
			_ = Task.Factory.StartNew(async () =>
			{
				using (IServiceScope scope = _scopeFactory.CreateScope())
				{
					SlashCommandService cmdService = scope.ServiceProvider.GetRequiredService<SlashCommandService>();
					await cmdService.HandleSlashCommandAsync(arg);
				}
			});
		}

		private async Task _client_Ready()
		{
			_logger.Information($"Client ready. Logged in as {_client.CurrentUser.Username}");
			using (var scope = _scopeFactory.CreateScope())
			{
				var cmdService = scope.ServiceProvider.GetRequiredService<SlashCommandService>();
				await cmdService.UpdateCommandsAsync();
			}
		}
	}
}
