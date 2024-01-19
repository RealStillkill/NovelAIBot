using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace NovelAIBot.Services
{
	internal class SlashCommandService
	{

		private readonly DiscordSocketClient _client;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger _logger;
		private readonly NovelAIService _aiService;

		public SlashCommandService(DiscordSocketClient client, IServiceScopeFactory scopeFactory, ILogger logger, NovelAIService aiService)
		{
			_client = client;
			_scopeFactory = scopeFactory;
			_logger = logger;
			_aiService = aiService;
		}

		public async Task HandleSlashCommandAsync(SocketSlashCommand cmd)
		{
			_logger.Information($"{cmd.User.Username} executed {cmd.CommandName}");
			switch (cmd.Data.Name)
			{
				case "prompt":
					await cmd.DeferAsync();
					_ = Task.Factory.StartNew(async () =>
					{
						await Prompt(cmd);
					});
					break;
			}
		}

		public async Task UpdateCommandsAsync()
		{
			_logger.Information("Updating commands.");
			await _client.BulkOverwriteGlobalApplicationCommandsAsync(GetCommands());
			_logger.Information("Commands updated.");
		}

		private async Task Prompt(SocketSlashCommand cmd)
		{
			await _aiService.AddPromptToQueueAsync(cmd);
		}

		private SlashCommandProperties[] GetCommands()
		{
			List<SlashCommandBuilder> commands = new List<SlashCommandBuilder>();
			#region Prompt
			SlashCommandBuilder promptBuilder = new SlashCommandBuilder();
			promptBuilder.WithName("prompt")
				.WithDescription("Send an image prompt to NovelAI. (You are responsible for deleting non-TOS images)")
				.WithDefaultPermission(false)
				.WithNsfw(true)
				.AddOption(new SlashCommandOptionBuilder()
					.WithName("prompt")
					.WithDescription("The desired image prompt")
					.WithType(ApplicationCommandOptionType.String)
					.WithRequired(true))
				.AddOption(new SlashCommandOptionBuilder()
					.WithName("negative-prompt")
					.WithDescription("Undesired image tags")
					.WithType(ApplicationCommandOptionType.String)
					.WithRequired(false))
				.AddOption(new SlashCommandOptionBuilder()
					.WithName("image-size")
					.WithDescription("Generate a different image size?")
					.WithRequired(false)
					.WithType(ApplicationCommandOptionType.Integer)
					.AddChoice("portrait", 0)
					.AddChoice("landscape", 1)
					.AddChoice("square", 2)
					.AddChoice("mobile-wallpaper", 3));
			commands.Add(promptBuilder);
			#endregion

			List<SlashCommandProperties> builtCommands = new List<SlashCommandProperties>();
			foreach (SlashCommandBuilder builder in commands)
			{
				builtCommands.Add(builder.Build());
			}

			return builtCommands.ToArray();
		}
	}
}
