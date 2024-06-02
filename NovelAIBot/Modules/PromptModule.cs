using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NovelAIBot.Enums;
using NovelAIBot.Models;
using NovelAIBot.Services;

namespace NovelAIBot.Modules
{
	internal class PromptModule : InteractionModuleBase<SocketInteractionContext>
	{
		private string AuthKey { get => _configuration.GetRequiredSection("GenerationApi")["ApiKey"]; }

		private readonly ILogger<PromptModule> _logger;
		private readonly QueueService _queueService;
		private readonly IConfiguration _configuration;

		public PromptModule(ILogger<PromptModule> logger, QueueService queueService, IConfiguration configuration)
		{
			_logger = logger;
			_queueService = queueService;
			_configuration = configuration;
		}

		[SlashCommand("prompt", "Generates an image based on a prompt")]
		public async Task Prompt(
			[Summary(description:"The prompt to base the image off of.")][MaxLength(500)]string prompt,
			[Summary(description:"The aspect ratio of the image.")]ImageSizes imageSize = ImageSizes.Portrait,
			[Summary(description: "Tags to exclude from the prompt.")][MaxLength(500)]string negativePrompt = null)
		{
			await DeferAsync();

#if DEBUG
			if (Context.User.Id != 253313886466473997)
			{
				_logger.LogDebug($"{Context.User.Username} tried to use the bot but it's in debug mode!");
				await FollowupAsync("I'm still working on it. Go away.");
				return;
			}
#endif

			_logger.LogInformation($"{Context.User.Username} used prompt. Prompt: {prompt}, Negative: {negativePrompt}, Size: {Enum.GetName(imageSize)}.");
			int width;
			int height;
			switch (imageSize)
			{
				case ImageSizes.Portrait:
					width = 832;
					height = 1216;
					break;
				case ImageSizes.Landscape:
					width = 1216;
					height = 832;
					break;
				case ImageSizes.Square:
					width = 960;
					height = 960;
					break;
				case ImageSizes.Mobile:
					width = 704;
					height = 1472;
					break;
				default:
					width = 832;
					height = 1216;
					break;
			}

			if (_configuration.GetRequiredSection("GenerationApi")["Mode"] == "Contained")
			{
				NaiRequest request = new NaiRequest(prompt, negativePrompt, height, width, Context);
				await _queueService.AddPromptToQueueAsync(request);
			}
			else
			{
				BackendRequest request = new BackendRequest(prompt, negativePrompt, AuthKey, Context, height, width);
				await _queueService.AddPromptToQueueAsync(request);
			}
		}

		[ComponentInteraction("delete-image")]
		public async Task DeleteImage()
		{
			await DeferAsync(true);
			await Context.Interaction.ModifyOriginalResponseAsync(x =>
			{
				x.Embed = null;
				x.Content = $"Image deleted by {Context.User.Username}";
				x.Attachments = null;
				x.Components = null;
			});
			await FollowupAsync("Image deleted.", ephemeral: true);
		}
	}
}