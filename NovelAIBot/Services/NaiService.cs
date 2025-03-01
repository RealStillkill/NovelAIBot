using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NovelAIBot.Enums;
using NovelAIBot.JsonModels;
using NovelAIBot.Models;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace NovelAIBot.Services
{
	internal class NaiService : IGenerationService
	{
		private readonly ILogger<NaiService> _logger;
		private readonly IConfiguration _configuration;
		private readonly HttpClient _httpClient;
		public NaiService(ILogger<NaiService> logger, IConfiguration configuration)
		{
			_logger = logger;
			_configuration = configuration;
			_httpClient = new HttpClient();
			_httpClient.BaseAddress = new Uri("https://image.novelai.net/ai/");
			string token = _configuration.GetSection("GenerationApi")["ApiKey"] 
				?? throw new Exception("No api key defined");
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		}

		public async Task<byte[]> GetImageBytesAsync(INaiRequest request)
		
		{
			var configSection = _configuration.GetSection("GenerationApi");
			//string defaultPositive = configSection["DefaultPositive"] ?? string.Empty;
			//string defaultNegative = configSection["DefaultNegative"] ?? string.Empty;
			
			ImageGenerationRequest imageRequest = new ImageGenerationRequest(request.Prompt, request.NegativePrompt);
			imageRequest.Parameters.Height = request.Height;
			imageRequest.Parameters.Width = request.Width;

			string json = JsonConvert.SerializeObject(imageRequest);
			StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync("/ai/generate-image", content);
			response.EnsureSuccessStatusCode();
			byte[] zip = await response.Content.ReadAsByteArrayAsync();
			byte[] image = GetImageFromZip(zip);
			return image;
		}

		private byte[] GetImageFromZip(byte[] zip)
		{
			using (MemoryStream ms = new MemoryStream(zip))
			using (ZipArchive archive = new ZipArchive(ms))
			{
				var entry = archive.Entries.First();
				using MemoryStream outStream = new MemoryStream();
				entry.Open().CopyTo(outStream);
				return outStream.ToArray();
			}
		}
	}
}
