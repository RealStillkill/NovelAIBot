using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NovelAIBot.Extensions;
using NovelAIBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NovelAIBot.Services
{
	internal class BackendService : IGenerationService
	{
		public event EventHandler<BackendQueueStatus> BackendQueueStatusChanged;

		private string WebSocketBaseAddress { get { return _configuration.GetSection("GenerationApi")["BackendUrl"]; } }
		private string ImageBaseAddress { get { return _configuration.GetSection("GenerationApi")["BackendImageUrl"]; } }



		private readonly IConfiguration _configuration;
		private readonly ILogger<BackendService> _logger;
		private readonly HttpClient _httpClient;

		public BackendService(IConfiguration configuration, ILogger<BackendService> logger)
		{
			_configuration = configuration;
			_logger = logger;
			_httpClient = new HttpClient();
			_httpClient.BaseAddress = new Uri(ImageBaseAddress);
		}


		public async Task<byte[]> GetImageBytesAsync(INaiRequest request)
		{
			var configSection = _configuration.GetSection("GenerationApi");
			string defaultPositive = configSection["DefaultPositive"] ?? string.Empty;
			string defaultNegative = configSection["DefaultNegative"] ?? string.Empty;

			BackendRequest bRequest = (BackendRequest)request;
			//bRequest.AppendToPrompt(defaultPositive);
			//bRequest.AppendToNegativePrompt(defaultNegative);


			using ClientWebSocket client = new ClientWebSocket();
			await client.ConnectAsync(new Uri(WebSocketBaseAddress), CancellationToken.None);
			string json = JsonSerializer.Serialize(bRequest);

			await client.SendTextMessageAsync(json, Encoding.UTF8);
			json = await client.ReceiveTextMessageAsync(1024 * 20, Encoding.UTF8);
			BackendQueueStatus status = JsonSerializer.Deserialize<BackendQueueStatus>(json);

			do
			{
				json = await client.ReceiveTextMessageAsync(1024 * 20, Encoding.UTF8);
				BackendQueueStatus newStatus = JsonSerializer.Deserialize<BackendQueueStatus>(json);
				if (newStatus.QueuePosition != status.QueuePosition || newStatus.State != status.State)
					BackendQueueStatusChanged?.Invoke(this, newStatus);
				status = newStatus;
			} while (status.State == NaiQueueState.Enqueued || status.State == NaiQueueState.Processing);

			if (status.State == NaiQueueState.CompletedError)
			{
				await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Request complete.", CancellationToken.None);
				throw new Exception("Image generation completed in an error state");
			}

			await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Request complete.", CancellationToken.None);

			return await _httpClient.GetByteArrayAsync($"/api/nai/getimage/{status.Id}");
		}
	}
}