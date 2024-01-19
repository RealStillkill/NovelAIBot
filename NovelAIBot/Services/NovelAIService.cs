using Discord.WebSocket;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace NovelAIBot.Services
{
	internal class NovelAIService
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly Queue<SocketSlashCommand> queue = new Queue<SocketSlashCommand>();
		private readonly ILogger _logger;


		//private Dictionary<SocketInter>

		private bool isBusy = false;

		private event EventHandler<SocketSlashCommand> JobFinished;

		private SocketSlashCommand currentRequest;

		public NovelAIService(IConfiguration config, ILogger logger)
		{
			_configuration = config;
			_logger = logger;

			_httpClient = new HttpClient();
			_httpClient.BaseAddress = new Uri("https://api.novelai.net");

			string token = _configuration["NovelAIToken"];
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
			this.JobFinished += NovelAIService_JobFinished;
		}


		public async Task AddPromptToQueueAsync(SocketSlashCommand cmd)
		{
			if (queue.Count >= 5)
			{
				await cmd.FollowupAsync("Queue full. Wait for some jobs to complete.", ephemeral: true);
				return;
			}

			string prompt = (string)cmd.Data.Options.First(x => x.Name == "prompt");

			if (!isBusy && queue.Count == 0)
			{
				await cmd.FollowupAsync($"Prompt job started. 0 prompts ahead.\n**Prompt:** {prompt}");
				_ = Task.Factory.StartNew(async () => await StartJob(cmd));
			}

			else
			{
				queue.Enqueue(cmd);
				await cmd.FollowupAsync($"Prompt job queued. {queue.Count} prompts ahead. ~10 seconds per prompt\n{prompt}");
			}
		}

		public async Task ClearJobsAsync(SocketMessageComponent button)
		{
			var jobs = queue.ToArray();
			this.queue.Clear();

			Func<SocketSlashCommand, Task<SocketSlashCommand>> modifyResponse = async (cmd) =>
			{
				await cmd.ModifyOriginalResponseAsync(x =>
				{
					x.Content = "Request cancelled. Job queue cleared.";
				});
				return cmd;
			};

			if (currentRequest != null)
				await modifyResponse(currentRequest);

			for (int i = 0; i < jobs.Length; i++)
			{
				await modifyResponse(jobs[i]);
			}
			this.isBusy = false;
			await button.FollowupAsync($"Queue cleared by {button.User}.");
		}

		private async Task StartJob(SocketSlashCommand cmd)
		{
			isBusy = true;
			currentRequest = cmd;

			Func<string, string> escapeUnderscoreFormatting = (input) =>
			{
				return input.Replace("_", "\\_");
			};


			string prompt = (string)cmd.Data.Options.First(x => x.Name == "prompt");
			string negPrompt = string.Empty;
			if (cmd.Data.Options.Any(x => x.Name == "negative-prompt"))
			{
				negPrompt = (string)cmd.Data.Options.First(x => x.Name == "negative-prompt");
			}
			ImageSizes size = ImageSizes.Portrait;
			if (cmd.Data.Options.Any(x => x.Name == "image-size"))
			{
				int val = Convert.ToInt32(cmd.Data.Options.First(x => x.Name == "image-size").Value);
				size = (ImageSizes)val;
			}
			await cmd.ModifyOriginalResponseAsync(x =>
			{
				x.Content = $"Prompt job started. 0 prompts ahead.\n**Prompt:** {escapeUnderscoreFormatting(prompt)}\n\n**Negative Prompt:** {escapeUnderscoreFormatting(negPrompt)}\n\n**Image Size**: {Enum.GetName(typeof(ImageSizes), (int)size)}";
			});
			try
			{
				byte[] imageData = await GetImageFromServer(prompt, negPrompt, cmd.User.Username, size);
				ButtonBuilder deleteButtonBuilder = new ButtonBuilder();
				deleteButtonBuilder
					.WithCustomId("button.ai-delete-image")
					.WithStyle(ButtonStyle.Danger)
					.WithEmote(new Emoji("🗑️"))
					.WithLabel("Delete TOS Image");


				ButtonBuilder clearButtonBuilder = new ButtonBuilder();
				clearButtonBuilder
					.WithCustomId("button.ai-clear-queue")
					.WithStyle(ButtonStyle.Primary)
					.WithEmote(new Emoji("🧹"))
					.WithLabel("Clear Queue");

				var components = new ComponentBuilder()
					.WithButton(deleteButtonBuilder)
					.WithButton(clearButtonBuilder)
					.Build();


				using (MemoryStream ms = new MemoryStream(imageData))
				{
					await cmd.ModifyOriginalResponseAsync(x =>
					{
						x.Content = $"Prompt job finished.\n**Prompt:** {escapeUnderscoreFormatting(prompt)}\n\n**Negative Prompt:** {escapeUnderscoreFormatting(negPrompt)}\n\n**Image Size**:  {Enum.GetName(typeof(ImageSizes), (int)size)}";
						x.Components = components;
						x.Attachments = new Optional<IEnumerable<FileAttachment>>(new FileAttachment[] { new FileAttachment(ms, $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png") });
					});
				}
			}
			catch (HttpRequestException httpreqex)
			{
				await cmd.ModifyOriginalResponseAsync(x =>
				{
					x.Content = $"{httpreqex.StatusCode} - {httpreqex.Message}";
				});
			}
			catch (Exception ex)
			{
				_logger.Error(ex.ToString());
			}
			finally
			{
				currentRequest = null;
				this.JobFinished?.Invoke(this, cmd);
			}
		}

		private async Task<byte[]> GetImageFromServer(string prompt, string negative, string user, ImageSizes size)
		{
			ImageGenerationRequest request = new ImageGenerationRequest(prompt + ", " + "best quality, amazing quality, very aesthetic, absurdres");

			if (negative != string.Empty)
				request.Parameters.NegativePrompt += $", {negative}, lowres, {{bad}}, error, fewer, extra, missing, worst quality, jpeg artifacts, bad quality, watermark, unfinished, displeasing, chromatic aberration, signature, extra digits, artistic error, username, scan, [abstract] ";

			switch (size)
			{
				case ImageSizes.Portrait:
					request.Parameters.Width = 832;
					request.Parameters.Height = 1216;
					break;
				case ImageSizes.Landscape:
					request.Parameters.Width = 1216;
					request.Parameters.Height = 832;
					break;
				case ImageSizes.Square:
					request.Parameters.Width = 960;
					request.Parameters.Height = 960;
					break;
				case ImageSizes.Mobile:
					request.Parameters.Width = 704;
					request.Parameters.Height = 1472;
					break;

			}

			string json = JsonConvert.SerializeObject(request);
			StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync("/ai/generate-image", content);
			response.EnsureSuccessStatusCode();
			byte[] zip = await response.Content.ReadAsByteArrayAsync();
			byte[] image = GetImageFromZip(zip);
			await LogImagePrompt(prompt, user);
			return image;
		}

		private void NovelAIService_JobFinished(object sender, SocketSlashCommand e)
		{
			if (queue.Count > 0)
				_ = Task.Factory.StartNew(async () => await StartJob(queue.Dequeue()));
			else isBusy = false;
			Console.WriteLine("Job finished");
		}

		private byte[] GetImageFromZip(byte[] zip)
		{
			using (MemoryStream ms = new MemoryStream(zip))
			using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
			{
				var entry = archive.Entries.First(x => x.Name.EndsWith(".png"));
				using (MemoryStream outStream = new MemoryStream())
				using (var stream = entry.Open())
				{
					stream.CopyTo(outStream);
					return outStream.ToArray();
				}
			}
		}

		private async Task LogImagePrompt(string prompt, string user)
		{
			long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

			string promptPath = Path.Combine(Directories.AppData, "novelprompts");
			if (!Directory.Exists(promptPath))
				Directory.CreateDirectory(promptPath);
		}

		private async void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			Func<SocketSlashCommand, Task<SocketSlashCommand>> modifyResponse = async (cmd) =>
			{
				await cmd.ModifyOriginalResponseAsync(x =>
				{
					x.Content = "Request cancelled. Bot is restarting.";
				});
				return cmd;
			};
			_ = Task.Factory.StartNew(async () => await modifyResponse(currentRequest));
			var jobs = queue.ToArray();
			queue.Clear();
			for (int i = 0; i < jobs.Length; i++)
			{
				_ = Task.Factory.StartNew(async () =>
				{
					await modifyResponse(jobs[i]);
				});
			}
		}

		public class ImageGenerationRequest
		{
			[JsonProperty("input")]
			public string Input { get; set; }

			[JsonProperty("model")]
			public string Model { get; set; }

			[JsonProperty("action")]
			public string Action { get; set; }

			[JsonProperty("parameters")]
			public Parameters Parameters { get; set; }

			[JsonProperty("url")]
			public string Url { get; set; }
			public ImageGenerationRequest(string input)
			{
				Input = input;
				Model = "nai-diffusion-3";
				Action = "generate";
				Parameters = new Parameters();
				Url = @"https://i8k38ax_hklkp.tenant-novelai.knative.chi.coreweave.com/SXb9P'&\\U(qsOAZb2]jIa5P Kn.sB 6>y$ig&E){y-#Cp/l4J/8FEhQIXEYw.{";
			}
		}

		public class Parameters
		{
			[JsonProperty("add_original_image")]
			public bool AddOriginalImage { get; set; } = false;
			[JsonProperty("cfg_rescale")]
			public float CfgRescale { get; set; } = 0.7f;
			[JsonProperty("controlnet_strength")]
			public float ControlNetStrength { get; set; } = 1.0f;
			[JsonProperty("dynamic_thresholding")]
			public bool DynamicThresholding { get; set; } = false;
			[JsonProperty("extra_noise_seed")]
			public long ExtraNoiseSeed { get; set; } = 1705183271038;
			[JsonProperty("height")]
			public int Height { get; set; } = 1216;
			[JsonProperty("legacy")]
			public bool Legacy { get; set; } = false;
			[JsonProperty("n_samples")]
			public int NSamples { get; set; } = 1;
			[JsonProperty("negative_prompt")]
			public string NegativePrompt { get; set; } = "lowres, bad anatomy";
			[JsonProperty("noise_schedule")]
			public string NoiseSchedule { get; set; } = "native";
			[JsonProperty("qualityToggle")]
			public bool QualityToggle { get; set; } = false;
			[JsonProperty("sampler")]
			public string Sampler { get; set; } = "k_euler_ancestral";
			[JsonProperty("scale")]
			public int Scale { get; set; } = 6;
			[JsonProperty("seed")]
			public long Seed { get => GetRandomSeed(); }
			[JsonProperty("sm")]
			public bool SM { get; set; } = false;
			[JsonProperty("sm_dyn")]
			public bool SMDYN { get; set; } = false;
			[JsonProperty("steps")]
			public int Steps { get; set; } = 28;
			[JsonProperty("ucPreset")]
			public int UcPreset { get; set; } = 3;
			[JsonProperty("uncond_scale")]
			public int UncondScale { get; set; } = 0;
			[JsonProperty("width")]
			public int Width { get; set; } = 832;
			private long GetRandomSeed()
				=> new Random().NextInt64(0, long.MaxValue);
		}

		private enum ImageSizes { Portrait = 0, Landscape = 1, Square = 2, Mobile = 3 }
	}
}
