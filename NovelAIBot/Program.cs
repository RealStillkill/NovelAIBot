using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using NovelAIBot.Services;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace NovelAIBot
{
	internal partial class Program
	{
		static void Main(string[] args)
			=> new Program().MainAsync(args).GetAwaiter().GetResult();

		private async Task MainAsync(string[] args)
		{
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console()
				.WriteTo.File(Path.Combine(Directories.AppData, "log.txt"))
				.CreateLogger();


			var client = new DiscordSocketClient(new DiscordSocketConfig
			{
				ConnectionTimeout = 8000,
				HandlerTimeout = 3000,
				MessageCacheSize = 25,
				LogLevel = LogSeverity.Verbose,
				GatewayIntents = GatewayIntents.All
			});

			IServiceProvider serviceProvider = new ServiceCollection()
				.AddSingleton(client)
				.AddSingleton<DiscordCoordinationService>()
				.AddSingleton<NovelAIService>()
				.AddSingleton<ILogger>(Log.Logger)
				.AddScoped<SlashCommandService>()
				.AddScoped<DiscordInteractionService>()
				.AddScoped<IConfiguration>(_ => GetConfiguration())
				.BuildServiceProvider();

			var coordination = serviceProvider.GetRequiredService<DiscordCoordinationService>();
			await Task.Delay(-1);
		}

		private IConfiguration GetConfiguration()
		{
			string cfgPath = Directories.Config;
#if DEBUG
			cfgPath = Directories.Config.Replace(".json", ".development.json");
#endif
			if (!Directory.Exists(Directories.AppData))
				Directory.CreateDirectory(Directories.AppData);

			if (!File.Exists(cfgPath))
			{
				ConfigModel config = new ConfigModel();
				File.WriteAllText(cfgPath, config.ToJson());
			}

			return new ConfigurationBuilder().AddJsonFile(cfgPath).Build();
		}
	}
}
