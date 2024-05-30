using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NovelAIBot.Services;

namespace NovelAIBot
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			HostApplicationBuilder builder = new HostApplicationBuilder();

			if (IsDebugMode())
			{
				builder.Configuration.AddJsonFile("appsettings.development.json", false, true);
			}
			else builder.Configuration.AddJsonFile("appsettings.json", false, true);

			string constring = builder.Configuration.GetConnectionString("DefaultConnection");
			builder.Logging.AddConsole();

			var client = new DiscordSocketClient(new DiscordSocketConfig
			{
				ConnectionTimeout = 8000,
				HandlerTimeout = 3000,
				MessageCacheSize = 25,
				LogLevel = LogSeverity.Debug,
				GatewayIntents = GatewayIntents.All
			});
			builder.Services.AddSingleton(client);
			builder.Services.AddSingleton(new InteractionService(client, new InteractionServiceConfig
			{
				LogLevel = LogSeverity.Debug,
				UseCompiledLambda = true
			}));
			builder.Services.AddSingleton<QueueService>();
			builder.Services.AddKeyedScoped<IGenerationService, NaiService>("Contained");


			builder.Services.AddHostedService<DiscordService>();
			var app = builder.Build();
			await app.RunAsync();
		}

		public static bool IsDebugMode()
		{
#if DEBUG
			return true;
#else
			return false;
#endif
		}
	}
}
