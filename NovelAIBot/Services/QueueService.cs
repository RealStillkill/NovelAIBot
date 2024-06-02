using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelAIBot.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NovelAIBot.Services
{
	internal class QueueService
	{
		public Queue<INaiRequest> Queue { get; init; } = new Queue<INaiRequest>();
		public bool IsBusy { get; private set; }

		private event EventHandler<INaiRequest> JobCompleted;

		private readonly ILogger<QueueService> _logger;
		private readonly IConfiguration _configuration;
		private readonly IServiceProvider _serviceProvider;
		public QueueService(ILogger<QueueService> logger, IConfiguration configuration, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_configuration = configuration;
			_serviceProvider = serviceProvider;

			JobCompleted += QueueService_JobCompleted;
		}

		private void QueueService_JobCompleted(object? sender, INaiRequest e)
		{
			if (Queue.Count == 0)
			{
				_logger.LogInformation("Queue empty");
				return;
			}

			_ = Task.Factory.StartNew(async () =>
			{
				INaiRequest request = Queue.Dequeue();
				await StartJob(request);
			});
		}

		public async Task AddPromptToQueueAsync(INaiRequest request)
		{
			var interaction = request.Context.Interaction;

			bool pSuccess = int.TryParse(_configuration.GetRequiredSection("GenerationApi")["QueueLength"], out int queueLength);
			if (!pSuccess)
				queueLength = 5;

			if (Queue.Count >= queueLength)
			{
				
				await interaction.FollowupAsync("Queue full. Wait for some jobs to complete.", ephemeral: true);
				return;
			}

			if (!IsBusy && Queue.Count == 0)
			{
				await request.Context.Interaction.FollowupAsync($"Prompt job started. 0 prompts ahead.\n**Prompt:** {request.Prompt}");
				_ = Task.Factory.StartNew(async () => await StartJob(request));
			}
			else
			{
				Queue.Enqueue(request);
				await request.Context.Interaction.FollowupAsync($"Prompt job queued. {Queue.Count} prompts ahead.\n**Prompt:**{request.Prompt}");
			}
		}

		private async Task StartJob(INaiRequest request)
		{
			this.IsBusy = true;
			if (request is NaiRequest)
				await StartContainedJob(request);
			if (request is BackendRequest)
				await StartBackendJob(request);


			this.IsBusy = false;
			this.JobCompleted?.Invoke(this, request);
		}

		private async Task StartBackendJob(INaiRequest request)
		{
			try
			{
				using IServiceScope scope = _serviceProvider.CreateScope();
				IGenerationService naiService = scope.ServiceProvider.GetRequiredKeyedService<IGenerationService>("Backend");

				byte[] image = await naiService.GetImageBytesAsync(request);

				await SendToDiscord(request, image);
				scope.Dispose();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while generating an image");
				await request.Context.Interaction.ModifyOriginalResponseAsync(x =>
				{
					x.Content = $"An error has occurred while processing your request:\n{ex.Message}";
				});
			}
		}


		private async Task StartContainedJob(INaiRequest request)
		{
			try
			{
				using IServiceScope scope = _serviceProvider.CreateScope();
				IGenerationService naiService = scope.ServiceProvider.GetRequiredKeyedService<IGenerationService>("Contained");

				byte[] image = await naiService.GetImageBytesAsync(request);
				await SendToDiscord(request, image);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while generating an image");
				await request.Context.Interaction.ModifyOriginalResponseAsync(x =>
				{
					x.Content = $"An error has occurred while processing your request:\n{ex.Message}";
				});
			}
		}

		private async Task SendToDiscord(INaiRequest request, byte[] image)
		{
			DefaultPrompts defaults = GetDefaultPrompts();
			FileAttachment attachment;
			using (MemoryStream ms = new MemoryStream(image))
			{
				attachment = new FileAttachment(ms, "image.png");
				EmbedBuilder embedBuilder = new EmbedBuilder()
					.WithTitle("Text2Image Generation")
					.WithAuthor(request.Context.User)
					.WithCurrentTimestamp()
					.WithImageUrl("attachment://image.png")
					.AddField("Prompt", request.Prompt.Replace(", " + defaults.Positive, string.Empty));


				string cleanNegative = request.NegativePrompt.Replace(defaults.Negative, string.Empty).Trim();
				if (cleanNegative.EndsWith(","))
					cleanNegative = cleanNegative.Remove(cleanNegative.Length - 1);
				if (!string.IsNullOrEmpty(cleanNegative))
					embedBuilder.AddField("Negative Prompt", cleanNegative);


				if (!string.IsNullOrEmpty(defaults.Positive))
					embedBuilder.AddField("Default Tags", defaults.Positive);
				if (!string.IsNullOrEmpty(defaults.Negative))
					embedBuilder.AddField("Default Negative Tags", defaults.Negative);

				embedBuilder.AddField("Size", $"{request.Width}x{request.Height}");

				await request.Context.Interaction.ModifyOriginalResponseAsync(x =>
				{
					x.Content = "";
					x.Attachments = new List<FileAttachment> { attachment };
					x.Embed = embedBuilder.Build();
					x.Components = GetMessageButtons();
				});
			}
		}

		private MessageComponent GetMessageButtons()
		{
			ComponentBuilder builder = new ComponentBuilder()
				.WithButton("Delete TOS Image", "delete-image", ButtonStyle.Danger);
			return builder.Build();
		}

		

		private DefaultPrompts GetDefaultPrompts()
		{
			var configSection = _configuration.GetSection("GenerationApi");
			string defaultPositive = configSection["DefaultPositive"] ?? string.Empty;
			string defaultNegative = configSection["DefaultNegative"] ?? string.Empty;

			return new DefaultPrompts(defaultPositive, defaultNegative);
		}

		internal struct DefaultPrompts
		{
			public string Positive { get; private set; }
			public string Negative { get; private set; }

			public DefaultPrompts(string positive, string negative)
			{
				Positive = positive;
				Negative = negative;
			}
		}
	}
}