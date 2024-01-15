using Discord.WebSocket;
using Serilog;
using System;
using System.Linq;

namespace NovelAIBot.Services
{
	internal class DiscordInteractionService
	{
		private readonly NovelAIService _aiService;
		private readonly DiscordSocketClient _client;
		private readonly ILogger _logger;

		public DiscordInteractionService(DiscordSocketClient client, ILogger logger, NovelAIService aiService)
		{
			_client = client;
			_logger = logger;
			_aiService = aiService;
		}

		public async Task HandleButtonAsync(SocketMessageComponent button)
		{
			switch (button.Data.CustomId)
			{
				case "button.ai-clear-queue":
					_ = Task.Factory.StartNew(async () => await ClearQueueAsync(button));
					break;
				case "button.ai-delete-image":
					_ = Task.Factory.StartNew(async () => await DeleteImageAsync(button));
					break;
			}
		}
		private async Task ClearQueueAsync(SocketMessageComponent button)
		{
			await button.DeferAsync();
			await _aiService.ClearJobsAsync(button);
		}
		private async Task DeleteImageAsync(SocketMessageComponent button)
		{
			await button.DeferAsync();
			try
			{

				await button.Message.ModifyAsync(x =>
				{
					x.Content = $"Image deleted by {button.User.Username}";
					x.Attachments = null;
					x.Components = null;
				});
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "An error occurred while deleting an image");
			}
			finally
			{
				await button.FollowupAsync();
			}
		}
	}
}
